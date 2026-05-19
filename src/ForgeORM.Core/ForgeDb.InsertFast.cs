using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Abstractions;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    private static readonly ConcurrentDictionary<Type, ForgeFastInsertPlan> FastInsertPlans = new();

    /// <summary>
    /// Inserts a single entity using ForgeORM's fast insert path.
    /// This path is designed for benchmark and high-throughput scenarios:
    /// scalar database columns only, identity columns excluded, no graph traversal, no navigation handling.
    /// </summary>
    public int InsertFast<T>(T entity)
    {
        return InsertFastAsync(entity).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Inserts a single entity using a cached insert plan, compiled property getters, and direct DbCommand execution.
    /// Navigation properties such as Customer or Items are ignored; use InsertGraphAsync for aggregate graph inserts.
    /// </summary>
    public async Task<int> InsertFastAsync<T>(
        T entity,
        CancellationToken cancellationToken = default)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        var plan = GetOrCreateFastInsertPlan(typeof(T));

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = plan.Sql;
        command.CommandType = CommandType.Text;

        for (var i = 0; i < plan.Properties.Length; i++)
        {
            var property = plan.Properties[i];
            var getter = plan.Getters[i];

            var value = getter(entity!);
            value = NormalizeFastInsertParameterValue(value, property);

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@" + property.Name;
            parameter.Value = value ?? DBNull.Value;

            ApplyProviderParameterType(parameter, value);

            command.Parameters.Add(parameter);
        }

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static ForgeFastInsertPlan GetOrCreateFastInsertPlan(Type type)
    {
        return FastInsertPlans.GetOrAdd(type, CreateFastInsertPlan);
    }

    private static ForgeFastInsertPlan CreateFastInsertPlan(Type type)
    {
        var table = ResolveFastInsertTableName(type);

        var properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .Where(IsFastInsertScalarColumn)
            .Where(p => !IsFastInsertIdentityColumn(p))
            .Where(p => p.GetCustomAttribute<ForgeComputedAttribute>() is null)
            .ToArray();

        if (properties.Length == 0)
            throw new InvalidOperationException($"No insertable scalar columns were found for entity '{type.Name}'.");

        var columns = properties
            .Select(p => p.GetCustomAttribute<ForgeColumnAttribute>()?.Name ?? p.Name)
            .ToArray();

        var parameters = properties
            .Select(p => "@" + p.Name)
            .ToArray();

        var sql =
            $"INSERT INTO {table} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", parameters)});";

        var getters = properties
            .Select(CreateFastInsertGetter)
            .ToArray();

        return new ForgeFastInsertPlan
        {
            Sql = sql,
            Properties = properties,
            Getters = getters
        };
    }

    private static Func<object, object?> CreateFastInsertGetter(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var cast = Expression.Convert(instance, property.DeclaringType!);
        var propertyAccess = Expression.Property(cast, property);
        var convert = Expression.Convert(propertyAccess, typeof(object));

        return Expression
            .Lambda<Func<object, object?>>(convert, instance)
            .Compile();
    }

    private static string ResolveFastInsertTableName(Type type)
    {
        return type.GetCustomAttribute<ForgeTableAttribute>()?.Name ?? type.Name;
    }

    private static bool IsFastInsertIdentityColumn(PropertyInfo property)
    {
        return property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)
            || property.GetCustomAttribute<ForgeKeyAttribute>() is not null;
    }

    private static bool IsFastInsertScalarColumn(PropertyInfo property)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (type.IsEnum)
            return true;

        return type.IsPrimitive
            || type == typeof(string)
            || type == typeof(Guid)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(DateOnly)
            || type == typeof(TimeOnly)
            || type == typeof(TimeSpan)
            || type == typeof(byte[]);
    }

    private static object? NormalizeFastInsertParameterValue(object? value, PropertyInfo property)
    {
        if (value is null)
            return null;

        if (value is DateTime dateTime)
        {
            if (dateTime == default || dateTime < new DateTime(1753, 1, 1))
                return DateTime.UtcNow;

            return dateTime;
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            if (dateTimeOffset == default)
                return DateTimeOffset.UtcNow;

            return dateTimeOffset;
        }

        if (value is DateOnly dateOnly)
        {
            return dateOnly.ToDateTime(TimeOnly.MinValue);
        }

        if (value is TimeOnly timeOnly)
        {
            return timeOnly.ToTimeSpan();
        }

        if (value.GetType().IsEnum)
        {
            // ForgeORM default enum strategy is numeric storage using the enum underlying type.
            // String storage is intentionally not used in the hot path.
            var enumType = value.GetType();
            var underlyingType = Enum.GetUnderlyingType(enumType);
            return Convert.ChangeType(value, underlyingType);
        }

        return value;
    }

    private static void ApplyProviderParameterType(DbParameter parameter, object? value)
    {
        if (parameter is not SqlParameter sqlParameter || value is null)
            return;

        switch (value)
        {
            case DateTime:
                sqlParameter.SqlDbType = SqlDbType.DateTime2;
                break;
            case DateTimeOffset:
                sqlParameter.SqlDbType = SqlDbType.DateTimeOffset;
                break;
            case TimeSpan:
                sqlParameter.SqlDbType = SqlDbType.Time;
                break;
            case Guid:
                sqlParameter.SqlDbType = SqlDbType.UniqueIdentifier;
                break;
        }
    }
}

internal sealed class ForgeFastInsertPlan
{
    public required string Sql { get; init; }

    public required PropertyInfo[] Properties { get; init; }

    public required Func<object, object?>[] Getters { get; init; }
}
