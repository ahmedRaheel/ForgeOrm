using System.Collections;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

internal static class ForgeSplitQueryBatching
{
    public const int DefaultInlineParameterLimit = 1800;

    /// <summary>
    /// Creates a provider-safe split-query id filter. Small collections keep ForgeORM's enumerable parameter expansion path.
    /// Large SQL Server collections are prepared for TVP/temp-table provider paths by using the same parameter bag contract.
    /// </summary>
    public static ForgeSplitIdFilter BuildIdFilter(string columnName, string parameterName, IReadOnlyList<object?> ids)
    {
        if (ids.Count == 0)
            return new ForgeSplitIdFilter("1 = 0", new Dictionary<string, object?>());

        // Keep SQL stable and let the provider binder expand/TVP this enumerable.
        return new ForgeSplitIdFilter($"{columnName} IN @{parameterName}", new Dictionary<string, object?> { [parameterName] = ids.ToArray() });
    }

    public static DataTable ToSqlServerIdTable(string columnName, IEnumerable ids)
    {
        var table = new DataTable();
        table.Columns.Add(columnName, typeof(object));
        foreach (var id in ids)
            table.Rows.Add(id ?? DBNull.Value);
        return table;
    }

    public static SqlParameter CreateSqlServerTvp(string parameterName, string typeName, DataTable table)
        => new(parameterName.StartsWith('@') ? parameterName : "@" + parameterName, SqlDbType.Structured)
        {
            TypeName = typeName,
            Value = table
        };
}
