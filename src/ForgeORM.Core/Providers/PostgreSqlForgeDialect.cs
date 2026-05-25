using System.Data;
using System.Text;

namespace ForgeORM.Core.Providers;

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
