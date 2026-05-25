using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.LowCode;

public static class ForgeLowCodeServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeLowCode operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeLowCode operation.</returns>
    public static IServiceCollection AddForgeLowCode(this IServiceCollection services) => services.AddSingleton<IForgeLowCodeEngine, ForgeLowCodeEngine>();
}
