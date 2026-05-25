using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
