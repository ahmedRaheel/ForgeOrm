using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Identity;

public sealed record ForgePrincipal(string UserId, IReadOnlyList<string> Roles, IReadOnlyDictionary<string,string> Claims);
public sealed record ForgePolicyRequirement(string Resource, string Action, string? TenantId = null);
public sealed record ForgePolicyDecision(bool Allowed, string Reason);

public interface IForgePolicyEngine
{
    ForgePolicyDecision Authorize(ForgePrincipal principal, ForgePolicyRequirement requirement);
}

public sealed class ForgePolicyEngine : IForgePolicyEngine
{
    public ForgePolicyDecision Authorize(ForgePrincipal principal, ForgePolicyRequirement requirement)
    {
        if (principal.Roles.Contains("Admin", StringComparer.OrdinalIgnoreCase)) return new(true, "Admin role allowed.");
        if (principal.Claims.TryGetValue($"perm:{requirement.Resource}", out var actions) && actions.Split(',').Contains(requirement.Action)) return new(true, "Explicit permission allowed.");
        return new(false, "No matching RBAC/ABAC policy.");
    }
}

public static class ForgeIdentityServiceCollectionExtensions
{
    public static IServiceCollection AddForgeIdentityPolicies(this IServiceCollection services) => services.AddSingleton<IForgePolicyEngine, ForgePolicyEngine>();
}

public sealed record AuthorizeRequest
{
    public required string UserId { get; init; }
    public required string Resource { get; init; }
    public required string Action { get; init; }
    public string? TenantId { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public IReadOnlyDictionary<string, string> Claims { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public DateTimeOffset RequestedAt { get; init; } = DateTimeOffset.UtcNow;

    public ForgePrincipal ToPrincipal() => new(UserId, Roles, Claims);
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
