using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.AI.Advanced;

public sealed record ForgeAiOptimizationRequest(string Sql, string Provider = "SqlServer", string? ExecutionPlan = null);
public sealed record ForgeAiOptimizationResult(IReadOnlyList<string> Issues, IReadOnlyList<string> Suggestions, IReadOnlyList<string> IndexRecommendations, string OptimizedSql);
public sealed record ForgeAiDiagnosticResult(string Severity, IReadOnlyList<string> Findings, IReadOnlyList<string> Fixes);
public sealed record ForgeGeneratedFile(string Path, string Content);
public sealed record ForgeMigrationPlan(string Name, IReadOnlyList<string> UpSql, IReadOnlyList<string> DownSql, IReadOnlyList<string> Warnings);

public interface IForgeAiOptimizer
{
    ForgeAiOptimizationResult Optimize(ForgeAiOptimizationRequest request);
}

public sealed class RuleBasedForgeAiOptimizer : IForgeAiOptimizer
{
    /// <summary>
    /// Initializes or executes the Optimize operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <returns>The operation result.</returns>
    public ForgeAiOptimizationResult Optimize(ForgeAiOptimizationRequest request)
    {
        var sql = request.Sql.Trim();
        var upper = sql.ToUpperInvariant();
        var issues = new List<string>();
        var suggestions = new List<string>();
        var indexes = new List<string>();

        if (upper.Contains("SELECT *")) { issues.Add("SELECT * detected."); suggestions.Add("Project only required columns to reduce IO and network cost."); }
        if (upper.Contains("LIKE '%")) { issues.Add("Leading wildcard LIKE detected."); suggestions.Add("Consider full-text search or a search index."); }
        if (upper.Contains("ORDER BY") && !upper.Contains("OFFSET") && !upper.Contains("TOP")) suggestions.Add("Consider pagination for ordered large result sets.");
        if (upper.Contains(" WHERE ")) indexes.Add("Review filtered columns and create covering indexes for WHERE + ORDER BY columns.");
        if (upper.Contains("JOIN")) indexes.Add("Ensure foreign-key join columns are indexed on both sides where appropriate.");

        return new ForgeAiOptimizationResult(issues, suggestions, indexes, sql);
    }
}

public interface IForgeAiDiagnostics
{
    ForgeAiDiagnosticResult Diagnose(string sql, TimeSpan elapsed, int rowCount);
}

public sealed class ForgeAiDiagnostics : IForgeAiDiagnostics
{
    /// <summary>
    /// Initializes or executes the Diagnose operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="elapsed">The elapsed value.</param>
    /// <param name="rowCount">The rowCount value.</param>
    /// <returns>The operation result.</returns>
    public ForgeAiDiagnosticResult Diagnose(string sql, TimeSpan elapsed, int rowCount)
    {
        var findings = new List<string>();
        var fixes = new List<string>();
        if (elapsed.TotalMilliseconds > 500) { findings.Add("Slow query detected."); fixes.Add("Check indexes, execution plan, pagination and projection size."); }
        if (rowCount > 5000) { findings.Add("Large result set detected."); fixes.Add("Use server-side pagination or streaming."); }
        if (sql.Contains("SELECT *", StringComparison.OrdinalIgnoreCase)) { findings.Add("Unbounded projection detected."); fixes.Add("Select explicit columns or DTO projection."); }
        var severity = findings.Count switch { 0 => "Healthy", 1 => "Warning", _ => "Critical" };
        return new ForgeAiDiagnosticResult(severity, findings, fixes);
    }
}

public interface IForgeAiCodeGenerator
{
    IReadOnlyList<ForgeGeneratedFile> GenerateMinimalApiCrud(string entityName, string routePrefix);
}

public sealed class ForgeAiCodeGenerator : IForgeAiCodeGenerator
{
    /// <summary>
    /// Initializes or executes the GenerateMinimalApiCrud operation.
    /// </summary>
    /// <param name="entityName">The entityName value.</param>
    /// <param name="routePrefix">The routePrefix value.</param>
    /// <returns>The operation result.</returns>
    public IReadOnlyList<ForgeGeneratedFile> GenerateMinimalApiCrud(string entityName, string routePrefix)
    {
        var code = $$"""
        public static class {{entityName}}Endpoints
        {
            /// <summary>
            /// Initializes or executes the Map{{entityName}}Endpoints operation.
            /// </summary>
            /// <param name="app">The app value.</param>
            /// <returns>The operation result.</returns>
            public static IEndpointRouteBuilder Map{{entityName}}Endpoints(this IEndpointRouteBuilder app)
            {
                var group = app.MapGroup("{{routePrefix}}").WithTags("{{entityName}}");
                group.MapGet("/", async (ForgeDb db) => await db.QueryAsync<{{entityName}}>("SELECT * FROM dbo.{{entityName}}s"));
                group.MapGet("/{id:int}", async (int id, ForgeDb db) => await db.QuerySingleOrDefaultAsync<{{entityName}}>("SELECT * FROM dbo.{{entityName}}s WHERE Id=@Id", new { Id = id }));
                return app;
            }
        }
        """;
        return [new ForgeGeneratedFile($"Generated/{entityName}Endpoints.cs", code)];
    }
}

public interface IForgeAiMigrationPlanner
{
    ForgeMigrationPlan PlanAddColumn(string table, string column, string sqlType, bool nullable = true);
}

public sealed class ForgeAiMigrationPlanner : IForgeAiMigrationPlanner
{
    /// <summary>
    /// Initializes or executes the PlanAddColumn operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="column">The column value.</param>
    /// <param name="sqlType">The sqlType value.</param>
    /// <param name="nullable">The nullable value.</param>
    /// <returns>The operation result.</returns>
    public ForgeMigrationPlan PlanAddColumn(string table, string column, string sqlType, bool nullable = true)
    {
        var nullability = nullable ? "NULL" : "NOT NULL";
        return new ForgeMigrationPlan(
            $"Add_{table}_{column}",
            [$"ALTER TABLE {table} ADD {column} {sqlType} {nullability};"],
            [$"ALTER TABLE {table} DROP COLUMN {column};"],
            nullable ? [] : [$"Column {column} is NOT NULL. Add DEFAULT or backfill strategy for existing rows."]);
    }
}

public interface IForgeAiSemanticQueryService
{
    Task<IReadOnlyList<ForgeVectorSearchResult>> SearchKnowledgeAsync(string text, CancellationToken cancellationToken = default);
}

public sealed class ForgeAiSemanticQueryService : IForgeAiSemanticQueryService
{
    private readonly IForgeVectorStore _store;
    /// <summary>
    /// Initializes or executes the ForgeAiSemanticQueryService operation.
    /// </summary>
    /// <param name="store">The store value.</param>
    public ForgeAiSemanticQueryService(IForgeVectorStore store) => _store = store;

    /// <summary>
    /// Initializes or executes the SearchKnowledgeAsync operation.
    /// </summary>
    /// <param name="text">The text value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public Task<IReadOnlyList<ForgeVectorSearchResult>> SearchKnowledgeAsync(string text, CancellationToken cancellationToken = default)
    {
        var vector = LocalEmbedding(text, 64);
        return _store.SearchAsync(vector, 5, cancellationToken);
    }

    private static float[] LocalEmbedding(string value, int dimensions)
    {
        var vector = new float[dimensions];
        foreach (var token in value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var hash = Math.Abs(token.ToUpperInvariant().GetHashCode());
            vector[hash % dimensions] += 1;
        }
        return vector;
    }
}

public static class ForgeAiAdvancedServiceCollectionExtensions
{
    /// <summary>
    /// Initializes or executes the AddForgeAdvancedAi operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The operation result.</returns>
    public static IServiceCollection AddForgeAdvancedAi(this IServiceCollection services)
    {
        services.AddSingleton<IForgeAiOptimizer, RuleBasedForgeAiOptimizer>();
        services.AddSingleton<IForgeAiDiagnostics, ForgeAiDiagnostics>();
        services.AddSingleton<IForgeAiCodeGenerator, ForgeAiCodeGenerator>();
        services.AddSingleton<IForgeAiMigrationPlanner, ForgeAiMigrationPlanner>();
        services.AddSingleton<IForgeAiSemanticQueryService, ForgeAiSemanticQueryService>();
        return services;
    }
}
