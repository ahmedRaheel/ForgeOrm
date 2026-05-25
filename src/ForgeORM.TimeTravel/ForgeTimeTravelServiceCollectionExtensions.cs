using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.TimeTravel;

public static class ForgeTimeTravelServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeTimeTravel operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeTimeTravel operation.</returns>
    public static IServiceCollection AddForgeTimeTravel(this IServiceCollection services) => services.AddSingleton<IForgeTimeTravelSqlBuilder, ForgeTimeTravelSqlBuilder>();
}
