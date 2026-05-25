using ForgeORM.Abstractions;
using ForgeORM.Core;
using MySqlConnector;
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
using static ForgeORM.Providers.MySql.ForgeProviderAdo;

namespace ForgeORM.Providers.MySql;

public sealed class MySqlForgeProvider : IForgeDatabaseProvider
{
    public string ProviderName => "MySql";

    public ForgeSqlDialect Dialect { get; } = new()
    {
        Name = "MySql",
        ParameterPrefix = "@",
        OpenIdentifier = "`",
        CloseIdentifier = "`"
    };

    public ForgeProviderCapabilities Capabilities { get; } = new()
    {
        SupportsBulkInsert = true,
        SupportsBulkUpdate = true,
        SupportsBulkDelete = true,
        SupportsBulkMerge = true,
        SupportsStoredProcedures = true,
        SupportsFunctions = true
    };

    // Thread-safe caches to ensure string generation happens exactly ONCE per entity shape
    private static readonly ConcurrentDictionary<RuntimeTypeHandle, string> InsertSqlCache = new();
    private static readonly ConcurrentDictionary<RuntimeTypeHandle, string> UpdateSqlCache = new();

    public DbConnection CreateConnection(string connectionString) => new MySqlConnection(connectionString);

    public ForgeCommand BuildGetById(ForgeEntityMetadata e, object id)
        => ForgeCommand.Text($"SELECT * FROM {e.TableName} WHERE {e.KeyColumn} = {Dialect.Parameter("Id")}", ForgeIdParameter<object?>.Create(id));

    public ForgeCommand BuildGetByCode(ForgeEntityMetadata e, string code)
        => ForgeCommand.Text($"SELECT * FROM {e.TableName} WHERE {e.CodeColumn} = {Dialect.Parameter("Code")}", new { Code = code });

    public ForgeCommand BuildGetByIds(ForgeEntityMetadata e, IReadOnlyCollection<int> ids)
        => ForgeCommand.Text($"SELECT * FROM {e.TableName} WHERE {e.KeyColumn} IN @Ids", new { Ids = ids });

    public ForgeCommand BuildInsert(ForgeEntityMetadata e, object entity)
    {
        var sql = InsertSqlCache.GetOrAdd(e.TypeHandle, static (_, metadata) =>
        {
            var props = metadata.Properties.Where(p => !p.IsComputed && !p.IsKey).ToList();
            var columns = string.Join(", ", props.Select(p => p.ColumnName));
            var values = string.Join(", ", props.Select(p => metadata.DialectParameterName(p.PropertyName)));
            return $"INSERT INTO {metadata.TableName} ({columns}) VALUES ({values})";
        }, e);

        return ForgeCommand.Text(sql, entity);
    }    

    public ForgeCommand BuildUpdate(ForgeEntityMetadata e, object entity)
    {
        // Pass e.EntityType.TypeHandle as the unique key, and 'e' as the factory state parameter
        var sql = UpdateSqlCache.GetOrAdd(e.EntityType.TypeHandle, static (typeHandle, metadata) =>
        {
            // 1. Filter updateable properties out of the metadata safely
            var props = metadata.Properties.Where(p => !p.IsComputed && !p.IsKey).ToList();

            // 2. Build the assignment string allocations inside the isolation layer
            var sets = string.Join(", ", props.Select(p => p.ColumnName + " = " + metadata.TableName(p.PropertyName)));

            // 3. Return the compiled immutable query string
            return $"UPDATE {metadata.TableName} SET {sets} WHERE {metadata.KeyColumn} = {metadata.DialectParameterName(metadata.KeyColumn)}";
        }, e);

        return ForgeCommand.Text(sql, entity);
    }

    public ForgeCommand BuildDelete(ForgeEntityMetadata e, object id)
        => ForgeCommand.Text($"DELETE FROM {e.TableName} WHERE {e.KeyColumn} = {Dialect.Parameter("Id")}", ForgeIdParameter<object?>.Create(id));

    public ForgeCommand BuildPage(ForgePageRequest r)
        => ForgeCommand.Text($"""SELECT * FROM ({r.Sql}) ForgePage ORDER BY {r.OrderBy} LIMIT {r.PageSize} OFFSET {r.Skip}""", r.Parameters);

    public ForgeCommand BuildCount(string baseSql, object? parameters = null)
        => ForgeCommand.Text($"SELECT COUNT(1) FROM ({baseSql}) ForgeCount", parameters);

    public ForgeCommand BuildBulkDelete(string tableName, string keyColumn, IReadOnlyCollection<int> ids)
        => ForgeCommand.Text($"DELETE FROM {tableName} WHERE {keyColumn} IN @Ids", new { Ids = ids });

    public ForgeCommand BuildFunctionScalar(string functionName, object? parameters = null)
        => ForgeCommand.Text($"SELECT {functionName}()", parameters);

    public ValueTask BulkInsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default)
        => MySqlNativeBulk.BulkInsertAsync(connection, tableName, rows, cancellationToken);

    public ValueTask BulkUpdateAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default)
    {
        if (rows is null || rows.Count == 0) return ValueTask.CompletedTask;
        return BulkFallback.UpdateAsync(connection, tableName, rows, keyColumn, cancellationToken);
    }

    public ValueTask BulkMergeAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default)
    {
        if (rows is null || rows.Count == 0) return ValueTask.CompletedTask;
        return BulkFallback.UpdateAsync(connection, tableName, rows, keyColumn, cancellationToken);
    }
}
