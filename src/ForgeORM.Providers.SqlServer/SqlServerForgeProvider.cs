using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ForgeORM.Abstractions;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Providers.SqlServer;

public sealed class SqlServerForgeProvider : IForgeDatabaseProvider
{
    public string ProviderName => "SqlServer";

    public ForgeSqlDialect Dialect { get; } = new()
    {
        Name = "SqlServer",
        ParameterPrefix = "@",
        OpenIdentifier = "[",
        CloseIdentifier = "]"
    };

    public ForgeProviderCapabilities Capabilities { get; } = new()
    {
        SupportsBulkInsert = true,
        SupportsBulkUpdate = true,
        SupportsBulkDelete = true,
        SupportsBulkMerge = true,
        SupportsStoredProcedures = true,
        SupportsFunctions = true,
        SupportsTableValuedParameters = true,
    };

    public DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

    public ForgeCommand BuildGetById(ForgeEntityMetadata entity, object id)
        => ForgeCommand.Text($"SELECT * FROM {entity.TableName} WHERE {entity.KeyColumn} = {Dialect.Parameter("Id")}", new { Id = id });

    public ForgeCommand BuildGetByCode(ForgeEntityMetadata entity, string code)
        => ForgeCommand.Text($"SELECT * FROM {entity.TableName} WHERE {entity.CodeColumn} = {Dialect.Parameter("Code")}", new { Code = code });

    public ForgeCommand BuildGetByIds(ForgeEntityMetadata entity, IReadOnlyCollection<int> ids)
    {
        if (ids is null || ids.Count == 0)
            return ForgeCommand.Text($"SELECT * FROM {entity.TableName} WHERE 1 = 0");
        return ForgeCommand.Text($"SELECT * FROM {entity.TableName} WHERE {entity.KeyColumn} IN @Ids", new { Ids = ids });
    }

    public ForgeCommand BuildInsert(ForgeEntityMetadata entity, object entityInstance)
        => ForgeCommand.Text(BuildInsertSql(entity), entityInstance);

    public ForgeCommand BuildUpdate(ForgeEntityMetadata entity, object entityInstance)
        => ForgeCommand.Text(BuildUpdateSql(entity), entityInstance);

    public ForgeCommand BuildDelete(ForgeEntityMetadata entity, object id)
        => ForgeCommand.Text($"DELETE FROM {entity.TableName} WHERE {entity.KeyColumn} = {Dialect.Parameter("Id")}", new { Id = id });

    public ForgeCommand BuildPage(ForgePageRequest request)
        => ForgeCommand.Text($"SELECT * FROM ({request.Sql}) ForgePage ORDER BY {request.OrderBy} OFFSET {request.Skip} ROWS FETCH NEXT {request.PageSize} ROWS ONLY", request.Parameters);

    public ForgeCommand BuildCount(string baseSql, object? parameters = null)
        => ForgeCommand.Text($"SELECT COUNT(1) FROM ({baseSql}) ForgeCount", parameters);

    public ForgeCommand BuildBulkDelete(string tableName, string keyColumn, IReadOnlyCollection<int> ids)
    {
        if (ids is null || ids.Count == 0)
            return ForgeCommand.Text($"DELETE FROM {tableName} WHERE 1 = 0");
        return ForgeCommand.Text($"DELETE FROM {tableName} WHERE {keyColumn} IN @Ids", new { Ids = ids });
    }

    public ForgeCommand BuildFunctionScalar(string functionName, object? parameters = null)
        => ForgeCommand.Text($"SELECT {functionName}()", parameters);

    public ValueTask BulkInsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default)
        => BulkFallback.InsertAsync(connection, tableName, rows, cancellationToken);

    public ValueTask BulkUpdateAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default)
        => BulkFallback.UpdateAsync(connection, tableName, rows, keyColumn, cancellationToken);

    public ValueTask BulkMergeAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default)
        => BulkFallback.UpdateAsync(connection, tableName, rows, keyColumn, cancellationToken);

    private string BuildInsertSql(ForgeEntityMetadata entity)
    {
        var properties = entity.Properties.Where(p => !p.IsComputed && !p.IsKey).ToArray();
        if (properties.Length == 0)
            throw new InvalidOperationException($"Entity '{entity.EntityType.Name}' has no insertable scalar properties.");

        var columns = string.Join(", ", properties.Select(p => p.ColumnName));
        var values = string.Join(", ", properties.Select(p => Dialect.Parameter(p.PropertyName)));
        return $"INSERT INTO {entity.TableName} ({columns}) VALUES ({values})";
    }

    private string BuildUpdateSql(ForgeEntityMetadata entity)
    {
        var properties = entity.Properties.Where(p => !p.IsComputed && !p.IsKey).ToArray();
        if (properties.Length == 0)
            throw new InvalidOperationException($"Entity '{entity.EntityType.Name}' has no updateable scalar properties.");

        var sets = string.Join(", ", properties.Select(p => p.ColumnName + " = " + Dialect.Parameter(p.PropertyName)));
        return $"UPDATE {entity.TableName} SET {sets} WHERE {entity.KeyColumn} = {Dialect.Parameter(entity.KeyColumn)}";
    }
}

internal static class BulkFallback
{
    private const string ParameterPrefix = "@";
    private static readonly ConcurrentDictionary<(Type Type, string Table), string> InsertSqlCache = new();
    private static readonly ConcurrentDictionary<(Type Type, string Table, string Key), string> UpdateSqlCache = new();

    public static ValueTask InsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken)
    {
        if (rows is null || rows.Count == 0)
            return default;

        var sql = InsertSqlCache.GetOrAdd((typeof(T), tableName), static key =>
        {
            var columns = ForgeProviderAdo.PropertyCache<T>.InsertColumns;
            if (columns.Length == 0)
                throw new InvalidOperationException($"Type {key.Type.Name} has no scalar properties to insert.");

            return $"INSERT INTO {key.Table} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", ForgeProviderAdo.PropertyCache<T>.InsertParameterNames)})";
        });

        return IgnoreResult(ForgeProviderAdo.ExecuteManyAsync(connection, sql, rows, ForgeProviderAdo.PropertyCache<T>.InsertProperties, cancellationToken));
    }

    public static ValueTask UpdateAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken)
    {
        if (rows is null || rows.Count == 0)
            return default;

        ArgumentException.ThrowIfNullOrWhiteSpace(keyColumn);

        var sql = UpdateSqlCache.GetOrAdd((typeof(T), tableName, keyColumn), static key =>
        {
            var updateProperties = ForgeProviderAdo.PropertyCache<T>.GetUpdateProperties(key.Key);
            if (updateProperties.Length == 0)
                throw new InvalidOperationException($"Type {key.Type.Name} has no scalar properties to update.");

            var setParts = new string[updateProperties.Length];
            for (var i = 0; i < updateProperties.Length; i++)
                setParts[i] = updateProperties[i].Info.Name + " = " + ParameterPrefix + updateProperties[i].Info.Name;

            return $"UPDATE {key.Table} SET {string.Join(", ", setParts)} WHERE {key.Key} = {ParameterPrefix}{key.Key}";
        });

        return IgnoreResult(ForgeProviderAdo.ExecuteManyAsync(connection, sql, rows, ForgeProviderAdo.PropertyCache<T>.Properties, cancellationToken));
    }

    private static async ValueTask IgnoreResult(ValueTask<int> task)
    {
        await task.ConfigureAwait(false);
    }
}

internal static class ForgeProviderAdo
{
    internal readonly struct CachedProperty
    {
        public CachedProperty(PropertyInfo info, string parameterName, Type declaredType)
        {
            Info = info;
            ParameterName = parameterName;
            DeclaredType = declaredType;
        }

        public PropertyInfo Info { get; }
        public string ParameterName { get; }
        public Type DeclaredType { get; }
    }

    internal static class PropertyCache<T>
    {
        public static readonly CachedProperty[] Properties = BuildProperties(includeIdentity: true);
        public static readonly CachedProperty[] InsertProperties = BuildProperties(includeIdentity: false);
        public static readonly string[] InsertColumns = InsertProperties.Select(p => p.Info.Name).ToArray();
        public static readonly string[] InsertParameterNames = InsertProperties.Select(p => p.ParameterName).ToArray();
        private static readonly ConcurrentDictionary<string, CachedProperty[]> UpdatePropertyCache = new(StringComparer.OrdinalIgnoreCase);

        public static CachedProperty[] GetUpdateProperties(string keyColumn)
            => UpdatePropertyCache.GetOrAdd(keyColumn, static key =>
                Properties.Where(p => !p.Info.Name.Equals(key, StringComparison.OrdinalIgnoreCase)).ToArray());

        private static CachedProperty[] BuildProperties(bool includeIdentity)
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var list = new List<CachedProperty>(properties.Length);

            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                if (!property.CanRead || !IsScalar(property.PropertyType))
                    continue;

                if (!includeIdentity && IsIdentityConvention(property))
                    continue;

                list.Add(new CachedProperty(property, property.Name, property.PropertyType));
            }

            return list.ToArray();
        }
    }

    public static ValueTask<int> ExecuteManyAsync<T>(DbConnection connection, string sql, IReadOnlyCollection<T> rows, CachedProperty[] properties, CancellationToken cancellationToken)
    {
        if (rows is null || rows.Count == 0 || properties.Length == 0)
            return new ValueTask<int>(0);

        return ExecuteManyInternalAsync(connection, sql, rows, properties, cancellationToken);
    }

    private static async ValueTask<int> ExecuteManyInternalAsync<T>(DbConnection connection, string sql, IReadOnlyCollection<T> rows, CachedProperty[] properties, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var dbParameters = new DbParameter[properties.Length];
        for (var i = 0; i < properties.Length; i++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = properties[i].ParameterName;
            command.Parameters.Add(parameter);
            dbParameters[i] = parameter;
        }

        try { command.Prepare(); } catch { }

        var total = 0;
        foreach (var row in rows)
        {
            for (var i = 0; i < properties.Length; i++)
            {
                var metadata = properties[i];
                var rawValue = ForgeProviderAccessors.Get(metadata.Info, row!);
                dbParameters[i].Value = NormalizeValue(rawValue, metadata.DeclaredType) ?? DBNull.Value;
            }

            total += await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return total;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsScalar(Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;
        return actual.IsPrimitive
               || actual.IsEnum
               || actual == typeof(string)
               || actual == typeof(Guid)
               || actual == typeof(decimal)
               || actual == typeof(DateTime)
               || actual == typeof(DateTimeOffset)
               || actual == typeof(DateOnly)
               || actual == typeof(TimeOnly)
               || actual == typeof(TimeSpan)
               || actual == typeof(byte[]);
    }

    private static bool IsIdentityConvention(PropertyInfo property)
    {
        var entityId = property.DeclaringType?.Name + "Id";
        return property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)
               || property.Name.Equals(entityId, StringComparison.OrdinalIgnoreCase)
               || property.GetCustomAttributes().Any(attribute => attribute.GetType().Name is "ForgeKeyAttribute" or "KeyAttribute");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? NormalizeValue(object? value, Type declaredType)
    {
        if (value is null)
            return null;

        var actual = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

        if (actual.IsEnum)
            return value.ToString();

        if (actual == typeof(DateTime))
        {
            var dateTime = (DateTime)value;
            return dateTime == default || dateTime < new DateTime(1753, 1, 1) ? DateTime.UtcNow : dateTime;
        }

        if (actual == typeof(DateTimeOffset))
        {
            var dto = (DateTimeOffset)value;
            return dto == default ? DateTimeOffset.UtcNow : dto;
        }

        return value;
    }
}
