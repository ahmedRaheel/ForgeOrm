namespace ForgeORM.Intelligence;

public interface IForgeSqlIntelligence
{
    ForgeSqlSuggestionResult Suggest(string partialSql, ForgeSqlContext context);
    ForgeSqlCorrectionResult Correct(string sql, ForgeSqlContext context);
    ForgeSqlCompletionResult Complete(string partialSql, int cursorPosition, ForgeSqlContext context);
}

public sealed class ForgeSqlContext
{
    public string ProviderName { get; init; } = "SqlServer";
    public IReadOnlyList<ForgeTableSchema> Tables { get; init; } = [];
}

public sealed class ForgeTableSchema
{
    public required string Name { get; init; }
    public IReadOnlyList<string> Columns { get; init; } = [];
}

public sealed class ForgeSqlSuggestionResult
{
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public IReadOnlyList<string> Suggestions { get; init; } = [];
}

public sealed class ForgeSqlCorrectionResult
{
    public bool Changed { get; init; }
    public required string Sql { get; init; }
    public IReadOnlyList<string> Fixes { get; init; } = [];
}

public sealed class ForgeSqlCompletionResult
{
    public IReadOnlyList<ForgeSqlCompletionItem> Items { get; init; } = [];
}

public sealed class ForgeSqlCompletionItem
{
    public required string Label { get; init; }
    public required string InsertText { get; init; }
    public string Kind { get; init; } = "Keyword";
    public string? Description { get; init; }
}

public sealed class BasicForgeSqlIntelligence : IForgeSqlIntelligence
{
    private static readonly string[] Keywords = ["SELECT","FROM","WHERE","JOIN","LEFT JOIN","INNER JOIN","ORDER BY","GROUP BY","HAVING","INSERT","UPDATE","DELETE","COUNT","SUM","AVG","OFFSET","FETCH NEXT"];

    /// <summary>
    /// Initializes or executes the Suggest operation.
    /// </summary>
    /// <param name="partialSql">The partialSql value.</param>
    /// <param name="context">The context value.</param>
    /// <returns>The operation result.</returns>
    public ForgeSqlSuggestionResult Suggest(string partialSql, ForgeSqlContext context)
    {
        var warnings = new List<string>();
        var suggestions = new List<string>();
        if (partialSql.Contains("SELECT *", StringComparison.OrdinalIgnoreCase)) { warnings.Add("SELECT * detected."); suggestions.Add("Select explicit columns."); }
        if (partialSql.Contains("NOLOCK", StringComparison.OrdinalIgnoreCase)) { warnings.Add("NOLOCK detected."); suggestions.Add("Avoid NOLOCK unless dirty reads are acceptable."); }
        return new ForgeSqlSuggestionResult { Warnings = warnings, Suggestions = suggestions };
    }

    /// <summary>
    /// Initializes or executes the Correct operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="context">The context value.</param>
    /// <returns>The operation result.</returns>
    public ForgeSqlCorrectionResult Correct(string sql, ForgeSqlContext context)
    {
        var fixes = new List<string>();
        var corrected = sql.Trim();
        if (!corrected.EndsWith(";")) { corrected += ";"; fixes.Add("Added semicolon."); }
        return new ForgeSqlCorrectionResult { Changed = fixes.Count > 0, Sql = corrected, Fixes = fixes };
    }

    /// <summary>
    /// Initializes or executes the Complete operation.
    /// </summary>
    /// <param name="partialSql">The partialSql value.</param>
    /// <param name="cursorPosition">The cursorPosition value.</param>
    /// <param name="context">The context value.</param>
    /// <returns>The operation result.</returns>
    public ForgeSqlCompletionResult Complete(string partialSql, int cursorPosition, ForgeSqlContext context)
    {
        var items = Keywords.Select(x => new ForgeSqlCompletionItem { Label = x, InsertText = x, Kind = "Keyword" }).ToList();
        foreach (var table in context.Tables)
        {
            items.Add(new ForgeSqlCompletionItem { Label = table.Name, InsertText = table.Name, Kind = "Table" });
            items.AddRange(table.Columns.Select(c => new ForgeSqlCompletionItem { Label = c, InsertText = c, Kind = "Column", Description = table.Name }));
        }
        return new ForgeSqlCompletionResult { Items = items };
    }
}
