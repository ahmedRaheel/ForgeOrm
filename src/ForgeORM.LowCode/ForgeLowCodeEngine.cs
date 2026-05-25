using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.LowCode;

public sealed class ForgeLowCodeEngine : IForgeLowCodeEngine
{
    /// <summary>
    /// Executes the GenerateErp operation.
    /// </summary>
    /// <param name="businessDomain">The businessDomain value.</param>
    /// <param name="modules">The modules value.</param>
    /// <returns>The result of the GenerateErp operation.</returns>
    public GeneratedEnterpriseApp GenerateErp(string businessDomain, IReadOnlyList<string> modules)
    {
        var entities = modules.Select(m => new LowCodeEntity(m.Trim().Replace(" ", ""), [new("Id", "Guid", true), new("Name", "string", true), new("CreatedAt", "DateTimeOffset")])).ToList();
        var routes = entities.Select(e => $"/api/{e.Name.ToLowerInvariant()}").ToList();
        return new GeneratedEnterpriseApp($"{businessDomain} ERP", entities, modules, routes);
    }

    public string GenerateMinimalApi(LowCodeEntity entity) => $$"""
app.MapGet("/api/{{entity.Name.ToLowerInvariant()}}", async db => Results.Ok());
app.MapPost("/api/{{entity.Name.ToLowerInvariant()}}", async ({{entity.Name}} request) => Results.Created($"/api/{{entity.Name.ToLowerInvariant()}}/{request.Id}", request));
""";

    public string GenerateReactForm(LowCodeEntity entity) => $$"""
export function {{entity.Name}}Form() {
  return <form>{{string.Join("", entity.Fields.Select(f => $"<label>{f.DisplayName ?? f.Name}<input name=\"{f.Name}\" /></label>"))}}</form>;
}
""";
}
