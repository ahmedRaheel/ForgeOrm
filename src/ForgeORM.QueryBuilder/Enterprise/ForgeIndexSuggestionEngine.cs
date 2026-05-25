using System.Text.RegularExpressions;

namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Generates simple index suggestions from WHERE and ORDER BY clauses.
/// </summary>
public static class ForgeIndexSuggestionEngine
{
    /// <summary>
    /// Suggests indexes for a generated query.
    /// </summary>
    public static IReadOnlyList<string> Suggest(ForgeSqlQuery query)
    {
        var tableMatch = Regex.Match(query.Sql, @"FROM\s+(?<table>[\w\.]+)", RegexOptions.IgnoreCase);
        if (!tableMatch.Success)
        {
            return Array.Empty<string>();
        }

        var table = tableMatch.Groups["table"].Value;
        var whereColumns = Regex.Matches(query.Sql, @"(?<column>\w+)\s*(=|>|<|>=|<=|LIKE)")
            .Select(x => x.Groups["column"].Value)
            .Where(x => !string.Equals(x, "WHERE", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var orderMatch = Regex.Match(query.Sql, @"ORDER\s+BY\s+(?<column>\w+)", RegexOptions.IgnoreCase);
        if (orderMatch.Success)
        {
            whereColumns.Add(orderMatch.Groups["column"].Value);
        }

        if (whereColumns.Count == 0)
        {
            return Array.Empty<string>();
        }

        var indexName = $"IX_{table.Replace(".", "_")}_{string.Join("_", whereColumns.Distinct())}";
        return [$"CREATE INDEX {indexName} ON {table} ({string.Join(", ", whereColumns.Distinct())});"];
    }
}
