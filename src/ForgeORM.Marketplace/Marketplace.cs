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
    public IReadOnlyList<ForgeMarketplaceItem> Search(string? query = null, string? category = null) => _items.Where(x =>
        (string.IsNullOrWhiteSpace(query) || x.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Description.Contains(query, StringComparison.OrdinalIgnoreCase)) &&
        (string.IsNullOrWhiteSpace(category) || x.Category.Equals(category, StringComparison.OrdinalIgnoreCase))).ToList();
    public void Publish(ForgeMarketplaceItem item) => _items.Add(item);
}

public static class ForgeMarketplaceServiceCollectionExtensions
{
    public static IServiceCollection AddForgeMarketplace(this IServiceCollection services) => services.AddSingleton<IForgeMarketplaceCatalog, InMemoryForgeMarketplaceCatalog>();
}
