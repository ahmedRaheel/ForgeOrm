using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.Rag;

public static class ForgeRagServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeRag operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeRag operation.</returns>
    public static IServiceCollection AddForgeRag(this IServiceCollection services)
    {
        services.AddSingleton<IForgeEmbeddingProvider, DeterministicEmbeddingProvider>();
        services.AddSingleton<IForgeRagEngine, ForgeRagEngine>();

        return services;
    }
}
