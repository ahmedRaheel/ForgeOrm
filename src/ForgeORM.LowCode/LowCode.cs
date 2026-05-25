using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.LowCode;

public sealed record LowCodeField(string Name, string Type, bool Required = false, string? DisplayName = null);
public sealed record LowCodeEntity(string Name, IReadOnlyList<LowCodeField> Fields);
public sealed record LowCodeApp(string Name, IReadOnlyList<LowCodeEntity> Entities, IReadOnlyList<LowCodePage> Pages);
public sealed record LowCodePage(string Name, string Route, string Entity, string Kind);
public sealed record GeneratedEnterpriseApp(string Name, IReadOnlyList<LowCodeEntity> Entities, IReadOnlyList<string> Modules, IReadOnlyList<string> ApiRoutes);

public interface IForgeLowCodeEngine
/// <summary>
/// Defines the GenerateErp operation.
/// </summary>
/// <param name="businessDomain">The businessDomain value.</param>
/// <param name="modules">The modules value.</param>
/// <returns>The result of the GenerateErp operation.</returns>
{
    /// <summary>
    /// Defines the GenerateErp operation.
    /// </summary>
    /// <param name="businessDomain">The businessDomain value.</param>
    /// <param name="modules">The modules value.</param>
    /// <returns>The result of the GenerateErp operation.</returns>
    GeneratedEnterpriseApp GenerateErp(string businessDomain, IReadOnlyList<string> modules);
    /// <summary>
    /// Defines the GenerateMinimalApi operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <returns>The result of the GenerateMinimalApi operation.</returns>
    string GenerateMinimalApi(LowCodeEntity entity);
    /// <summary>
    /// Defines the GenerateReactForm operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <returns>The result of the GenerateReactForm operation.</returns>
    string GenerateReactForm(LowCodeEntity entity);
}

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

public static class ForgeLowCodeServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeLowCode operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeLowCode operation.</returns>
    public static IServiceCollection AddForgeLowCode(this IServiceCollection services) => services.AddSingleton<IForgeLowCodeEngine, ForgeLowCodeEngine>();
}
public sealed record GenerateErpRequest
{
    public required string CompanyName { get; init; }

    public required string Industry { get; init; }

    public string? Description { get; init; }

    public IReadOnlyList<string> Modules { get; init; }
        = [];

    public IReadOnlyList<string> Databases { get; init; }
        = [];

    public bool GenerateApis { get; init; } = true;

    public bool GenerateFrontend { get; init; } = true;

    public bool GenerateReports { get; init; } = true;

    public bool GenerateWorkflows { get; init; } = true;

    public bool GenerateMultiTenancy { get; init; } = true;

    public bool GenerateAuditing { get; init; } = true;

    public bool GeneratePermissions { get; init; } = true;

    public bool GenerateDocker { get; init; } = true;

    public bool GenerateKubernetes { get; init; } = true;

    public bool GenerateTerraform { get; init; } = true;

    public string? PreferredDatabase { get; init; }

    public string? PreferredFrontend { get; init; }

    public string? PreferredArchitecture { get; init; }

    public string? AiProvider { get; init; }

    public string? TenantId { get; init; }

    public string? UserId { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public CancellationToken CancellationToken { get; init; }
}
