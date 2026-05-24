using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.VectorSearch;

public static class ForgeVectorServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeInMemoryVectorSearch operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeInMemoryVectorSearch operation.</returns>
    public static IServiceCollection AddForgeInMemoryVectorSearch(this IServiceCollection services)
    {
        services.AddSingleton<IForgeVectorStore, ForgeInMemoryVectorStore>();
        services.AddSingleton<ForgeVectorSqlBuilder>();
        return services;
    }
}
