using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.LowCode;

public sealed record LowCodeField(string Name, string Type, bool Required = false, string? DisplayName = null);
public sealed record LowCodeEntity(string Name, IReadOnlyList<LowCodeField> Fields);
public sealed record LowCodeApp(string Name, IReadOnlyList<LowCodeEntity> Entities, IReadOnlyList<LowCodePage> Pages);
public sealed record LowCodePage(string Name, string Route, string Entity, string Kind);
public sealed record GeneratedEnterpriseApp(string Name, IReadOnlyList<LowCodeEntity> Entities, IReadOnlyList<string> Modules, IReadOnlyList<string> ApiRoutes);

public interface IForgeLowCodeEngine
{
    GeneratedEnterpriseApp GenerateErp(string businessDomain, IReadOnlyList<string> modules);
    string GenerateMinimalApi(LowCodeEntity entity);
    string GenerateReactForm(LowCodeEntity entity);
}

public sealed class ForgeLowCodeEngine : IForgeLowCodeEngine
{
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

public static class ForgeLowCodeServiceCollectionExtensions
{
    public static IServiceCollection AddForgeLowCode(this IServiceCollection services) => services.AddSingleton<IForgeLowCodeEngine, ForgeLowCodeEngine>();
}
