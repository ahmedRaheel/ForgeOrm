using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.DataVirtualization;

public sealed record FederatedQuery(string Name, string Query, IReadOnlyList<string> Sources);
