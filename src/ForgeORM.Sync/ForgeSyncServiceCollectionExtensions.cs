using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Sync;

public static class ForgeSyncServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeOfflineSync operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeOfflineSync operation.</returns>
    public static IServiceCollection AddForgeOfflineSync(this IServiceCollection services) => services.AddSingleton<IForgeSyncEngine, ForgeSyncEngine>();
}
