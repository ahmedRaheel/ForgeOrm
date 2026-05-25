using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.Core;

/// <summary>Simple index advisor using rendered query metadata.</summary>
public static class ForgeQueryBuilderIndexSuggestionEngine
{
    public static ForgeQueryBuilderAnalysis Analyze<TEntity>(ForgeRenderedQuery query, IReadOnlyList<string> whereClauses, IReadOnlyList<string> orderClauses, string tableName)
    {
        var table = tableName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? typeof(TEntity).Name;
        var clean = table.Replace("dbo.", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("[", string.Empty).Replace("]", string.Empty);
        var suggestions = new List<string>();
        if (whereClauses.Count > 0 || orderClauses.Count > 0)
        {
            suggestions.Add($"CREATE INDEX IX_{clean}_Query ON {table} (/* WHERE columns first */) INCLUDE (/* projected columns */);");
        }
        if (query.Sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add("Place ORDER BY columns after equality-filter columns in composite indexes.");
        }
        if (query.Sql.Contains("LIKE", StringComparison.OrdinalIgnoreCase))
        {
            suggestions.Add("For leading-wildcard LIKE searches, consider full-text search or trigram indexes.");
        }
        return new ForgeQueryBuilderAnalysis(typeof(TEntity).Name, query.Sql, suggestions, ["Use actual execution plans before creating indexes in production."]);
    }
}
