using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.EventSourcing;

public static class ForgeEventSourcingServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeEventSourcing operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeEventSourcing operation.</returns>
    public static IServiceCollection AddForgeEventSourcing(this IServiceCollection services) => services.AddSingleton<IForgeEventStore, InMemoryForgeEventStore>();
}
