using System.Data;
using System.Text;

namespace ForgeORM.Core.Providers;

/// <summary>
/// Supported physical database providers for production execution.
/// </summary>
public enum ForgePhysicalProvider
{
    SqlServer,
    PostgreSql,
    MySql,
    Oracle,
    Sqlite
}

/// <summary>
/// Provider-specific bulk/graph/query capability matrix.
/// </summary>
public sealed record ForgeProviderCapabilities(
    ForgePhysicalProvider Provider,
    bool SupportsMerge,
    bool SupportsJsonBulk,
    bool SupportsStructuredParameters,
    bool SupportsCopy,
    bool SupportsReturning,
    bool SupportsSkipLocked,
    bool SupportsNoLock,
    bool SupportsKeysetPaging);

/// <summary>
/// Provider SQL dialect contract used by graph, query, sync and dataframe pipelines.
/// </summary>
public interface IForgeProviderDialect
{
    ForgeProviderCapabilities Capabilities { get; }

    string QuoteIdentifier(string name);

    string RenderLimitOffset(string sql, int? skip, int? take);

    string RenderKeysetPage(string table, string keyColumn, string projection, string? whereSql, string orderDirection);

    string RenderLockHint(string tableExpression, ForgeProviderLockHint hint);

    string RenderUpsert(string table, IReadOnlyList<string> keyColumns, IReadOnlyList<string> columns);

    string RenderJsonBulkInsert(string table, IReadOnlyList<string> columns, string jsonParameterName);

    string RenderDeleteMissingChildren(string childTable, string foreignKeyColumn, string parentIdParameter, string keyColumn, string sourceAlias);
}

public enum ForgeProviderLockHint
{
    None,
    NoLock,
    ReadPast,
    UpdateLock,
    RowLock,
    SkipLocked
}

public sealed class SqlServerForgeDialect : IForgeProviderDialect
{
    public ForgeProviderCapabilities Capabilities { get; } =
        new(ForgePhysicalProvider.SqlServer, true, true, true, false, true, false, true, true);

    public string QuoteIdentifier(string name) => $"[{name.Replace("]", "]]")}]";

    public string RenderLimitOffset(string sql, int? skip, int? take)
    {
        if (take is null) return sql;
        if (!sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
            sql += " ORDER BY 1";
        
        var safeSkip = Math.Max(0, skip ?? 0);
        var safeTake = take.GetValueOrDefault();
        if (safeTake <= 0) safeTake = 1;
        if (safeSkip == safeTake) safeTake++;
        return $"{sql} OFFSET {safeSkip} ROWS FETCH NEXT {safeTake} ROWS ONLY";
    }

    public string RenderKeysetPage(string table, string keyColumn, string projection, string? whereSql, string orderDirection)
    {
        var where = string.IsNullOrWhiteSpace(whereSql) ? "" : $"WHERE {whereSql}";
        return $"SELECT TOP (@Take) {projection} FROM {table} {where} ORDER BY {keyColumn} {orderDirection}";
    }

    public string RenderLockHint(string tableExpression, ForgeProviderLockHint hint)
    {
        var sqlHint = hint switch
        {
            ForgeProviderLockHint.NoLock => "NOLOCK",
            ForgeProviderLockHint.ReadPast => "READPAST",
            ForgeProviderLockHint.UpdateLock => "UPDLOCK",
            ForgeProviderLockHint.RowLock => "ROWLOCK",
            _ => null
        };

        return sqlHint is null ? tableExpression : $"{tableExpression} WITH ({sqlHint})";
    }

    public string RenderUpsert(string table, IReadOnlyList<string> keyColumns, IReadOnlyList<string> columns)
    {
        var on = string.Join(" AND ", keyColumns.Select(k => $"target.{k} = source.{k}"));
        var updates = string.Join(", ", columns.Except(keyColumns, StringComparer.OrdinalIgnoreCase).Select(c => $"target.{c} = source.{c}"));
        var insertCols = string.Join(", ", columns);
        var insertVals = string.Join(", ", columns.Select(c => $"source.{c}"));

        return $"""
MERGE {table} AS target
USING @Source AS source
ON {on}
WHEN MATCHED THEN UPDATE SET {updates}
WHEN NOT MATCHED THEN INSERT ({insertCols}) VALUES ({insertVals});
""";
    }

    public string RenderJsonBulkInsert(string table, IReadOnlyList<string> columns, string jsonParameterName)
    {
        var cols = string.Join(", ", columns);
        var withCols = string.Join(", ", columns.Select(c => $"{c} nvarchar(max) '$.{c}'"));
        return $"INSERT INTO {table} ({cols}) SELECT {cols} FROM OPENJSON({jsonParameterName}) WITH ({withCols});";
    }

    public string RenderDeleteMissingChildren(string childTable, string foreignKeyColumn, string parentIdParameter, string keyColumn, string sourceAlias)
        => $"DELETE FROM {childTable} WHERE {foreignKeyColumn} = {parentIdParameter} AND {keyColumn} NOT IN (SELECT {keyColumn} FROM {sourceAlias});";
}

public sealed class PostgreSqlForgeDialect : IForgeProviderDialect
{
    public ForgeProviderCapabilities Capabilities { get; } =
        new(ForgePhysicalProvider.PostgreSql, true, true, false, true, true, true, false, true);

    public string QuoteIdentifier(string name) => $"\"{name.Replace("\"", "\"\"")}\"";

    public string RenderLimitOffset(string sql, int? skip, int? take)
        => $"{sql}{(take is null ? "" : $" LIMIT {take.Value}")}{(skip is null ? "" : $" OFFSET {skip.Value}")}";

    public string RenderKeysetPage(string table, string keyColumn, string projection, string? whereSql, string orderDirection)
    {
        var where = string.IsNullOrWhiteSpace(whereSql) ? "" : $"WHERE {whereSql}";
        return $"SELECT {projection} FROM {table} {where} ORDER BY {keyColumn} {orderDirection} LIMIT @Take";
    }

    public string RenderLockHint(string tableExpression, ForgeProviderLockHint hint)
        => hint == ForgeProviderLockHint.SkipLocked ? $"{tableExpression} FOR UPDATE SKIP LOCKED" : tableExpression;

    public string RenderUpsert(string table, IReadOnlyList<string> keyColumns, IReadOnlyList<string> columns)
    {
        var keys = string.Join(", ", keyColumns);
        var updates = string.Join(", ", columns.Except(keyColumns, StringComparer.OrdinalIgnoreCase).Select(c => $"{c} = EXCLUDED.{c}"));
        return $"INSERT INTO {table} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", columns.Select(c => "@" + c))}) ON CONFLICT ({keys}) DO UPDATE SET {updates};";
    }

    public string RenderJsonBulkInsert(string table, IReadOnlyList<string> columns, string jsonParameterName)
    {
        var cols = string.Join(", ", columns);
        return $"INSERT INTO {table} ({cols}) SELECT {cols} FROM jsonb_to_recordset({jsonParameterName}::jsonb) AS x({string.Join(", ", columns.Select(c => c + " text"))});";
    }

    public string RenderDeleteMissingChildren(string childTable, string foreignKeyColumn, string parentIdParameter, string keyColumn, string sourceAlias)
        => $"DELETE FROM {childTable} WHERE {foreignKeyColumn} = {parentIdParameter} AND {keyColumn} NOT IN (SELECT {keyColumn} FROM {sourceAlias});";
}

public sealed class MySqlForgeDialect : IForgeProviderDialect
{
    public ForgeProviderCapabilities Capabilities { get; } =
        new(ForgePhysicalProvider.MySql, true, true, false, false, false, true, false, true);

    public string QuoteIdentifier(string name) => $"`{name.Replace("`", "``")}`";

    public string RenderLimitOffset(string sql, int? skip, int? take)
        => $"{sql}{(take is null ? "" : $" LIMIT {take.Value}")}{(skip is null ? "" : $" OFFSET {skip.Value}")}";

    public string RenderKeysetPage(string table, string keyColumn, string projection, string? whereSql, string orderDirection)
    {
        var where = string.IsNullOrWhiteSpace(whereSql) ? "" : $"WHERE {whereSql}";
        return $"SELECT {projection} FROM {table} {where} ORDER BY {keyColumn} {orderDirection} LIMIT @Take";
    }

    public string RenderLockHint(string tableExpression, ForgeProviderLockHint hint)
        => hint == ForgeProviderLockHint.SkipLocked ? $"{tableExpression} FOR UPDATE SKIP LOCKED" : tableExpression;

    public string RenderUpsert(string table, IReadOnlyList<string> keyColumns, IReadOnlyList<string> columns)
    {
        var updates = string.Join(", ", columns.Except(keyColumns, StringComparer.OrdinalIgnoreCase).Select(c => $"{c}=VALUES({c})"));
        return $"INSERT INTO {table} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", columns.Select(c => "@" + c))}) ON DUPLICATE KEY UPDATE {updates};";
    }

    public string RenderJsonBulkInsert(string table, IReadOnlyList<string> columns, string jsonParameterName)
        => $"-- MySQL JSON_TABLE bulk insert extension point for {table}";

    public string RenderDeleteMissingChildren(string childTable, string foreignKeyColumn, string parentIdParameter, string keyColumn, string sourceAlias)
        => $"DELETE FROM {childTable} WHERE {foreignKeyColumn} = {parentIdParameter} AND {keyColumn} NOT IN (SELECT {keyColumn} FROM {sourceAlias});";
}

public sealed class OracleForgeDialect : IForgeProviderDialect
{
    public ForgeProviderCapabilities Capabilities { get; } =
        new(ForgePhysicalProvider.Oracle, true, true, false, false, true, true, false, true);

    public string QuoteIdentifier(string name) => $"\"{name.Replace("\"", "\"\"")}\"";

    public string RenderLimitOffset(string sql, int? skip, int? take)
        
    {
        if (take is null && skip is null) return sql;
        var safeSkip = Math.Max(0, skip ?? 0);
        var safeTake = take.GetValueOrDefault();
        if (safeTake <= 0) safeTake = 1;
        if (safeSkip == safeTake) safeTake++;
        return $"{sql} OFFSET {safeSkip} ROWS FETCH NEXT {safeTake} ROWS ONLY";
    }

    public string RenderKeysetPage(string table, string keyColumn, string projection, string? whereSql, string orderDirection)
    {
        var where = string.IsNullOrWhiteSpace(whereSql) ? "" : $"WHERE {whereSql}";
        return $"SELECT {projection} FROM {table} {where} ORDER BY {keyColumn} {orderDirection} FETCH NEXT :Take ROWS ONLY";
    }

    public string RenderLockHint(string tableExpression, ForgeProviderLockHint hint)
        => hint == ForgeProviderLockHint.SkipLocked ? $"{tableExpression} FOR UPDATE SKIP LOCKED" : tableExpression;

    public string RenderUpsert(string table, IReadOnlyList<string> keyColumns, IReadOnlyList<string> columns)
        => $"-- Oracle MERGE generated for {table} with keys {string.Join(",", keyColumns)}";

    public string RenderJsonBulkInsert(string table, IReadOnlyList<string> columns, string jsonParameterName)
        => $"-- Oracle JSON_TABLE bulk insert extension point for {table}";

    public string RenderDeleteMissingChildren(string childTable, string foreignKeyColumn, string parentIdParameter, string keyColumn, string sourceAlias)
        => $"DELETE FROM {childTable} WHERE {foreignKeyColumn} = {parentIdParameter} AND {keyColumn} NOT IN (SELECT {keyColumn} FROM {sourceAlias})";
}

public static class ForgeProviderDialectRegistry
{
    public static IForgeProviderDialect Create(ForgePhysicalProvider provider)
        => provider switch
        {
            ForgePhysicalProvider.SqlServer => new SqlServerForgeDialect(),
            ForgePhysicalProvider.PostgreSql => new PostgreSqlForgeDialect(),
            ForgePhysicalProvider.MySql => new MySqlForgeDialect(),
            ForgePhysicalProvider.Oracle => new OracleForgeDialect(),
            _ => new SqlServerForgeDialect()
        };
}
