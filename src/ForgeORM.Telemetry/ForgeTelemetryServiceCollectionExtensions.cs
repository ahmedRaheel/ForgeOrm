using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Telemetry;

public static class ForgeTelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeTelemetry operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeTelemetry operation.</returns>
    public static IServiceCollection AddForgeTelemetry(this IServiceCollection services)
    {
        services.AddSingleton<IForgeTelemetry, ForgeTelemetry>();
        return services;
    }
}
