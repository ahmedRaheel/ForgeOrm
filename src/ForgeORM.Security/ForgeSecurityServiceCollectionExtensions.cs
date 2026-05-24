using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Security;

public static class ForgeSecurityServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeSecurity operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeSecurity operation.</returns>
    public static IServiceCollection AddForgeSecurity(this IServiceCollection services)
    {
        services.AddSingleton<IForgeSqlSecurityValidator, ForgeSqlSecurityValidator>();
        services.AddSingleton<IForgeDataMasker, ForgeDataMasker>();
        services.AddSingleton<IForgeColumnEncryptor, ForgeAesColumnEncryptor>();
        return services;
    }
}
