using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Marketplace;

public interface IForgeMarketplaceCatalog
/// <summary>
/// Defines the Search operation.
/// </summary>
/// <param name="query">The query value.</param>
/// <param name="category">The category value.</param>
/// <returns>The result of the Search operation.</returns>
{
    /// <summary>
    /// Defines the Search operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="category">The category value.</param>
    /// <returns>The result of the Search operation.</returns>
    IReadOnlyList<ForgeMarketplaceItem> Search(string? query = null, string? category = null);
    /// <summary>
    /// Defines the Publish operation.
    /// </summary>
    /// <param name="item">The item value.</param>
    void Publish(ForgeMarketplaceItem item);
}
