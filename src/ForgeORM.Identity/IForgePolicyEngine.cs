using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Identity;

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
