using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Marketplace;

public sealed record ForgeMarketplaceItem(string Id, string Name, string Category, string Version, string Author, string Description, IReadOnlyList<string> Tags);

public interface IForgeMarketplaceCatalog
{
    IReadOnlyList<ForgeMarketplaceItem> Search(string? query = null, string? category = null);
    void Publish(ForgeMarketplaceItem item);
}

public sealed class InMemoryForgeMarketplaceCatalog : IForgeMarketplaceCatalog
{
    private readonly List<ForgeMarketplaceItem> _items = [];
    /// <summary>
    /// Initializes or executes the Search operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="category">The category value.</param>
    /// <returns>The operation result.</returns>
    public IReadOnlyList<ForgeMarketplaceItem> Search(string? query = null, string? category = null) => _items.Where(x =>
        (string.IsNullOrWhiteSpace(query) || x.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Description.Contains(query, StringComparison.OrdinalIgnoreCase)) &&
        (string.IsNullOrWhiteSpace(category) || x.Category.Equals(category, StringComparison.OrdinalIgnoreCase))).ToList();
    /// <summary>
    /// Initializes or executes the Publish operation.
    /// </summary>
    /// <param name="item">The item value.</param>
    public void Publish(ForgeMarketplaceItem item) => _items.Add(item);
}

public static class ForgeMarketplaceServiceCollectionExtensions
{
    /// <summary>
    /// Initializes or executes the AddForgeMarketplace operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The operation result.</returns>
    public static IServiceCollection AddForgeMarketplace(this IServiceCollection services) => services.AddSingleton<IForgeMarketplaceCatalog, InMemoryForgeMarketplaceCatalog>();
}
