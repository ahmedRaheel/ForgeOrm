using System.Data;
using System.Text;

namespace ForgeORM.Core.Providers;

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
