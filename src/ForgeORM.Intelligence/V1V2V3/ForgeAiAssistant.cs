using ForgeORM.Abstractions;

namespace ForgeORM.Intelligence;

public sealed class ForgeAiAssistant : IForgeAiQueryClient, IForgeApiGenerator, IForgeMigrationPlanner, IForgeSchemaScaffolder
{
    public Task<ForgeAiQueryResult> GenerateSqlAsync(ForgeAiQueryRequest request, CancellationToken cancellationToken = default)
    {
        var entity = string.IsNullOrWhiteSpace(request.EntityName) ? "Records" : request.EntityName;
        var table = string.IsNullOrWhiteSpace(request.Schema) ? entity : $"{request.Schema}.{entity}";
        var prompt = request.Prompt.Trim();

        var where = prompt.Contains("active", StringComparison.OrdinalIgnoreCase)
            ? " WHERE IsActive = 1"
            : string.Empty;

        var take = prompt.Contains("top 10", StringComparison.OrdinalIgnoreCase)
            ? " TOP 10"
            : string.Empty;

        var sql = request.Provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
            ? $"SELECT{take} * FROM {table}{where}"
            : $"SELECT * FROM {table}{where}{(take.Length > 0 ? " LIMIT 10" : string.Empty)}";

        var warnings = new List<string>
        {
            "AI fallback mode generated a safe starter SQL. Review before production execution.",
            "Use parameterized filters when applying user-provided values."
        };

        var indexes = where.Length > 0
            ? new[] { $"CREATE INDEX IX_{entity}_IsActive ON {table}(IsActive);" }
            : Array.Empty<string>();

        return Task.FromResult(new ForgeAiQueryResult(
            sql,
            $"Generated a deterministic starter query for prompt: {prompt}",
            warnings,
            indexes));
    }

    public IReadOnlyList<ForgeGeneratedFile> GenerateCrudApi(ForgeApiGenerationRequest request)
    {
        var route = request.RoutePrefix.Trim('/');
        var code = $$"""
        namespace {{request.Namespace}};

        public static class {{request.EntityName}}Endpoints
        {
            public static IEndpointRouteBuilder Map{{request.EntityName}}Endpoints(this IEndpointRouteBuilder app)
            {
                var group = app.MapGroup("/{{route}}").WithTags("{{request.EntityName}}");
                group.MapGet("/", () => Results.Ok("List {{request.EntityName}}"));
                group.MapGet("/{id:int}", (int id) => Results.Ok($"Get {{request.EntityName}} {id}"));
                group.MapPost("/", () => Results.Created($"/{{route}}", null));
                group.MapPut("/{id:int}", (int id) => Results.Ok($"Update {{request.EntityName}} {id}"));
                group.MapDelete("/{id:int}", (int id) => Results.Ok($"Delete {{request.EntityName}} {id}"));
                return app;
            }
        }
        """;

        return [new ForgeGeneratedFile($"{request.EntityName}Endpoints.cs", code)];
    }

    public ForgeMigrationPlan Plan(string name, IReadOnlyList<string> currentSchema, IReadOnlyList<string> targetSchema)
    {
        var up = targetSchema.Except(currentSchema, StringComparer.OrdinalIgnoreCase).ToList();
        var down = currentSchema.Except(targetSchema, StringComparer.OrdinalIgnoreCase).ToList();

        return new ForgeMigrationPlan(
            name,
            up.Count == 0 ? ["-- No schema changes detected."] : up,
            down.Count == 0 ? ["-- No rollback changes detected."] : down);
    }

    public Task<IReadOnlyList<ForgeGeneratedFile>> ScaffoldAsync(ForgeScaffoldRequest request, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ForgeGeneratedFile> files =
        [
            new("README.scaffold.md", $"Scaffold request accepted for provider {request.Provider}. Add provider metadata reader to generate entities.")
        ];

        return Task.FromResult(files);
    }
}
