using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Cloud;

public static class ForgeCloudServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeCloudDeployment operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeCloudDeployment operation.</returns>
    public static IServiceCollection AddForgeCloudDeployment(this IServiceCollection services) => services.AddSingleton<IForgeDeploymentGenerator, ForgeDeploymentGenerator>();
}
