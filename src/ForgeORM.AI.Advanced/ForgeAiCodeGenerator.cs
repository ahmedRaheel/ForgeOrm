using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.AI.Advanced;

public sealed class ForgeAiCodeGenerator : IForgeAiCodeGenerator
{
    /// <summary>
    /// Executes the GenerateMinimalApiCrud operation.
    /// </summary>
    /// <param name="entityName">The entityName value.</param>
    /// <param name="routePrefix">The routePrefix value.</param>
    /// <returns>The result of the GenerateMinimalApiCrud operation.</returns>
    public IReadOnlyList<ForgeGeneratedFile> GenerateMinimalApiCrud(string entityName, string routePrefix)
    {
        var code = $$"""
        public static class {{entityName}}Endpoints
        {
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
