using System.Data;
using System.Text;

namespace ForgeORM.Core.Providers;

public sealed class OracleForgeDialect : IForgeProviderDialect
{
    public ForgeProviderCapabilities Capabilities { get; } =
        new(ForgePhysicalProvider.Oracle, true, true, false, false, true, true, false, true);

    public string QuoteIdentifier(string name) => $"\"{name.Replace("\"", "\"\"")}\"";

    public string RenderLimitOffset(string sql, int? skip, int? take)
        => take is null ? sql : $"{sql} OFFSET {skip ?? 0} ROWS FETCH NEXT {take.Value} ROWS ONLY";

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
