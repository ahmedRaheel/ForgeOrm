using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace ForgeORM.Core;

/// <summary>
/// Basic SQL/index analysis helper for expression-generated queries.
/// </summary>
public static class ForgeIndexSuggestionEngine
{
    public static ForgeQueryAnalysis Analyze<TEntity>(
        string sql,
        string tableName,
        IReadOnlyList<string> whereClauses,
        IReadOnlyList<string> orderClauses)
        where TEntity : class, new()
    {
        var whereColumns = ExtractWhereColumns(whereClauses);
        var orderColumns = ExtractOrderColumns(orderClauses);
        var suggested = BuildSuggestions(tableName, whereColumns, orderColumns);

        return new ForgeQueryAnalysis
        {
            Entity = typeof(TEntity).Name,
            TableName = tableName,
            Sql = sql,
            WhereColumns = whereColumns,
            OrderByColumns = orderColumns,
            SuggestedIndexes = suggested,
            Notes =
            [
                "Put equality/filter columns first.",
                "Put range columns after equality columns.",
                "Put ORDER BY columns after filter columns when the query sorts large result sets.",
                "Review actual execution plan before applying indexes in production."
            ]
        };
    }

    private static IReadOnlyList<string> ExtractWhereColumns(IReadOnlyList<string> whereClauses)
    {
        var columns = new List<string>();

        foreach (var clause in whereClauses)
        {
            foreach (Match match in Regex.Matches(
                clause,
                @"\b([A-Za-z_][A-Za-z0-9_]*)\b\s*(=|<>|>|>=|<|<=|LIKE|IN)\s*@",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                var column = match.Groups[1].Value;
                if (!columns.Contains(column, StringComparer.OrdinalIgnoreCase))
                {
                    columns.Add(column);
                }
            }
        }

        return columns;
    }

    private static IReadOnlyList<string> ExtractOrderColumns(IReadOnlyList<string> orderClauses)
    {
        var columns = new List<string>();

        foreach (var order in orderClauses)
        {
            var column = order
                .Replace(" DESC", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" ASC", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (!string.IsNullOrWhiteSpace(column) &&
                !columns.Contains(column, StringComparer.OrdinalIgnoreCase))
            {
                columns.Add(column);
            }
        }

        return columns;
    }

    private static IReadOnlyList<string> BuildSuggestions(
        string tableName,
        IReadOnlyList<string> whereColumns,
        IReadOnlyList<string> orderColumns)
    {
        var indexColumns = whereColumns
            .Concat(orderColumns)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (indexColumns.Length == 0)
        {
            return
            [
                "No specific index suggestion. Add WHERE or ORDER BY columns to generate index advice."
            ];
        }

        var safeTable = tableName
            .Replace("[", "", StringComparison.Ordinal)
            .Replace("]", "", StringComparison.Ordinal)
            .Replace("dbo.", "", StringComparison.OrdinalIgnoreCase)
            .Replace(".", "_", StringComparison.Ordinal)
            .Replace(" ", "_", StringComparison.Ordinal);

        var indexName = $"IX_{safeTable}_{string.Join("_", indexColumns)}";
        var columns = string.Join(", ", indexColumns);

        return
        [
            $"CREATE INDEX {indexName} ON {tableName} ({columns});"
        ];
    }
}
