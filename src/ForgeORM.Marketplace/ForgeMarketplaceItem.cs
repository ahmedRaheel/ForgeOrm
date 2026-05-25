using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Marketplace;

public sealed record ForgeMarketplaceItem(string Id, string Name, string Category, string Version, string Author, string Description, IReadOnlyList<string> Tags);
