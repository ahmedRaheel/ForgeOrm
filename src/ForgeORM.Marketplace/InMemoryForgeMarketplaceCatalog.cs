using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Marketplace;

public sealed class InMemoryForgeMarketplaceCatalog : IForgeMarketplaceCatalog
{
    private readonly List<ForgeMarketplaceItem> _items = [];
    /// <summary>
    /// Executes the Search operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="category">The category value.</param>
    /// <returns>The result of the Search operation.</returns>
    public IReadOnlyList<ForgeMarketplaceItem> Search(string? query = null, string? category = null) => _items.Where(x =>
        (string.IsNullOrWhiteSpace(query) || x.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Description.Contains(query, StringComparison.OrdinalIgnoreCase)) &&
        (string.IsNullOrWhiteSpace(category) || x.Category.Equals(category, StringComparison.OrdinalIgnoreCase))).ToList();
    /// <summary>
    /// Executes the Publish operation.
    /// </summary>
    /// <param name="item">The item value.</param>
    public void Publish(ForgeMarketplaceItem item) => _items.Add(item);
}
