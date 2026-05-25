using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.DataVirtualization;

public sealed record VirtualDataSource(string Name, string Kind, string ConnectionName);
