using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Identity;

public sealed record ForgePrincipal(string UserId, IReadOnlyList<string> Roles, IReadOnlyDictionary<string,string> Claims);
public sealed record ForgePolicyRequirement(string Resource, string Action, string? TenantId = null);
public sealed record ForgePolicyDecision(bool Allowed, string Reason);

public interface IForgePolicyEngine
/// <summary>
/// Defines the Authorize operation.
/// </summary>
/// <param name="principal">The principal value.</param>
/// <param name="requirement">The requirement value.</param>
/// <returns>The result of the Authorize operation.</returns>
{
    /// <summary>
    /// Defines the Authorize operation.
    /// </summary>
    /// <param name="principal">The principal value.</param>
    /// <param name="requirement">The requirement value.</param>
    /// <returns>The result of the Authorize operation.</returns>
    ForgePolicyDecision Authorize(ForgePrincipal principal, ForgePolicyRequirement requirement);
}

public sealed class ForgePolicyEngine : IForgePolicyEngine
{
    /// <summary>
    /// Executes the Authorize operation.
    /// </summary>
    /// <param name="principal">The principal value.</param>
    /// <param name="requirement">The requirement value.</param>
    /// <returns>The result of the Authorize operation.</returns>
    public ForgePolicyDecision Authorize(ForgePrincipal principal, ForgePolicyRequirement requirement)
    {
        if (principal.Roles.Contains("Admin", StringComparer.OrdinalIgnoreCase)) return new(true, "Admin role allowed.");
        if (principal.Claims.TryGetValue($"perm:{requirement.Resource}", out var actions) && actions.Split(',').Contains(requirement.Action)) return new(true, "Explicit permission allowed.");
        return new(false, "No matching RBAC/ABAC policy.");
    }
}

public static class ForgeIdentityServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeIdentityPolicies operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeIdentityPolicies operation.</returns>
    public static IServiceCollection AddForgeIdentityPolicies(this IServiceCollection services) => services.AddSingleton<IForgePolicyEngine, ForgePolicyEngine>();
}

public sealed record AuthorizeRequest
{
    /// <summary>
    /// Executes the string operation.
    /// </summary>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <returns>The result of the string operation.</returns>
    public required string UserId { get; init; }
    /// <summary>
    /// Executes the string operation.
    /// </summary>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <returns>The result of the string operation.</returns>
    public required string Resource { get; init; }
    /// <summary>
    /// Executes the string operation.
    /// </summary>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <returns>The result of the string operation.</returns>
    public required string Action { get; init; }
    /// <summary>
    /// Executes the string operation.
    /// </summary>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <returns>The result of the string operation.</returns>
    public string? TenantId { get; init; }
    /// <summary>
    /// Executes the string operation.
    /// </summary>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <returns>The result of the string operation.</returns>
    public IReadOnlyList<string> Roles { get; init; } = [];
    /// <summary>
    /// Executes the string operation.
    /// </summary>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <typeparam name="string">The type used by the operation.</typeparam>
    /// <returns>The result of the string operation.</returns>
    public IReadOnlyDictionary<string, string> Claims { get; init; } = new Dictionary<string, string>();
    /// <summary>
    /// Executes the ToPrincipal operation.
    /// </summary>
    /// <returns>The result of the ToPrincipal operation.</returns>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
    /// <summary>
    /// Executes the ToPrincipal operation.
    /// </summary>
    /// <returns>The result of the ToPrincipal operation.</returns>
    public string? IpAddress { get; init; }
    /// <summary>
    /// Executes the ToPrincipal operation.
    /// </summary>
    /// <returns>The result of the ToPrincipal operation.</returns>
    public string? UserAgent { get; init; }
    /// <summary>
    /// Executes the ToPrincipal operation.
    /// </summary>
    /// <returns>The result of the ToPrincipal operation.</returns>
    public DateTimeOffset RequestedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Executes the ToPrincipal operation.
    /// </summary>
    /// <returns>The result of the ToPrincipal operation.</returns>
    public ForgePrincipal ToPrincipal() => new(UserId, Roles, Claims);
    /// <summary>
    /// Executes the ToRequirement operation.
    /// </summary>
    /// <returns>The result of the ToRequirement operation.</returns>
    public ForgePolicyRequirement ToRequirement() => new(Resource, Action, TenantId);
}

public sealed record AuthorizationResult
{
    public bool IsAuthorized { get; init; }
    public string? Policy { get; init; }
    public string? Reason { get; init; }
    public IReadOnlyList<string> MissingRoles { get; init; } = [];
    public IReadOnlyList<string> MissingClaims { get; init; } = [];
}
