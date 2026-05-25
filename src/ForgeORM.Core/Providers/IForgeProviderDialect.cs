using System.Data;
using System.Text;

namespace ForgeORM.Core.Providers;

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
