using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Reflection;
using ForgeORM.Abstractions;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    private static readonly ConcurrentDictionary<Type, ForgeCompiledInsertPlan> CompiledInsertPlans = new();

    private int InsertCompiled<T>(T entity)
    {
        return InsertCompiledAsync(entity).GetAwaiter().GetResult();
    }

    private async ValueTask<int> InsertCompiledAsync<T>(
        T entity,
        CancellationToken cancellationToken = default)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        var plan = GetOrCreateCompiledInsertPlan(typeof(T));

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
            value = NormalizeCompiledInsertParameterValue(value, property);

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@" + property.Name;
            parameter.Value = value ?? DBNull.Value;

            ApplyProviderParameterType(parameter, value);

            command.Parameters.Add(parameter);
        }

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static ForgeCompiledInsertPlan GetOrCreateCompiledInsertPlan(Type type)
    {
        return CompiledInsertPlans.GetOrAdd(type, CreateCompiledInsertPlan);
    }

    private static ForgeCompiledInsertPlan CreateCompiledInsertPlan(Type type)
    {
        var table = ResolveCompiledInsertTableName(type);

        var properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .Where(IsCompiledInsertScalarColumn)
            .Where(p => !IsCompiledInsertIdentityColumn(p))
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
            .Select(CreateCompiledInsertGetter)
            .ToArray();

        return new ForgeCompiledInsertPlan
        {
            Sql = sql,
            Properties = properties,
            Getters = getters
        };
    }

    private static Func<object, object?> CreateCompiledInsertGetter(PropertyInfo property)
        => ForgeRuntimeAccessorCache.Getter(property);

    private static string ResolveCompiledInsertTableName(Type type)
    {
        return type.GetCustomAttribute<ForgeTableAttribute>()?.Name ?? type.Name;
    }

    private static bool IsCompiledInsertIdentityColumn(PropertyInfo property)
    {
        return property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)
            || property.GetCustomAttribute<ForgeKeyAttribute>() is not null;
    }

    private static bool IsCompiledInsertScalarColumn(PropertyInfo property)
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

    private static object? NormalizeCompiledInsertParameterValue(object? value, PropertyInfo property)
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
            return dateOnly.ToDateTime(TimeOnly.MinValue);

        if (value is TimeOnly timeOnly)
            return timeOnly.ToTimeSpan();

        if (value.GetType().IsEnum)
            return ForgeEnumBox.ToUnderlying(value);

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
