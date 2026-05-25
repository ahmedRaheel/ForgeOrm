using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.AI.Advanced;

public static class ForgeAiAdvancedServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeAdvancedAi operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeAdvancedAi operation.</returns>
    public static IServiceCollection AddForgeAdvancedAi(this IServiceCollection services)
    {
        services.AddSingleton<IForgeAiOptimizer, RuleBasedForgeAiOptimizer>();
        services.AddSingleton<IForgeAiDiagnostics, ForgeAiDiagnostics>();
        services.AddSingleton<IForgeAiCodeGenerator, ForgeAiCodeGenerator>();
        services.AddSingleton<IForgeAiMigrationPlanner, ForgeAiMigrationPlanner>();
        services.AddSingleton<IForgeAiSemanticQueryService, ForgeAiSemanticQueryService>();
        return services;
    }
}
