using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.DataVirtualization;

public static class ForgeDataVirtualizationServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeDataVirtualization operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeDataVirtualization operation.</returns>
    public static IServiceCollection AddForgeDataVirtualization(this IServiceCollection services) => services.AddSingleton<IForgeFederatedQueryPlanner, ForgeFederatedQueryPlanner>();
}
