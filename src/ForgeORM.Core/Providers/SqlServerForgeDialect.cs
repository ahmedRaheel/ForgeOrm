using System.Data;
using System.Text;

namespace ForgeORM.Core.Providers;

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
        return $"{sql} OFFSET {skip ?? 0} ROWS FETCH NEXT {take.Value} ROWS ONLY";
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
