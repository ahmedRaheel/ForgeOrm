using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Identity;

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
