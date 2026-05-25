using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.AI.Memory;

public static class ForgeAiMemoryServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeAiMemory operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeAiMemory operation.</returns>
    public static IServiceCollection AddForgeAiMemory(this IServiceCollection services) => services.AddSingleton<IForgeAiMemoryStore, InMemoryForgeAiMemoryStore>();
}
