using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Marketplace;

public static class ForgeMarketplaceServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeMarketplace operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeMarketplace operation.</returns>
    public static IServiceCollection AddForgeMarketplace(this IServiceCollection services) => services.AddSingleton<IForgeMarketplaceCatalog, InMemoryForgeMarketplaceCatalog>();
}
