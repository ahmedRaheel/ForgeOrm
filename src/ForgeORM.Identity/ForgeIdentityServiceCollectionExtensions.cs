using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Identity;

public static class ForgeIdentityServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeIdentityPolicies operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeIdentityPolicies operation.</returns>
    public static IServiceCollection AddForgeIdentityPolicies(this IServiceCollection services) => services.AddSingleton<IForgePolicyEngine, ForgePolicyEngine>();
}
