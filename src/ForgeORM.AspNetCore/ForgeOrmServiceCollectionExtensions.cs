using ForgeORM.Abstractions;
using ForgeORM.Analytics;
using ForgeORM.Core;
using ForgeORM.Intelligence;
using ForgeORM.Mapping;
using ForgeORM.NextGen;
using ForgeORM.Providers.MySql;
using ForgeORM.Providers.Oracle;
using ForgeORM.Providers.PostgreSql;
using ForgeORM.Providers.Sqlite;
using ForgeORM.Providers.SqlServer;
using ForgeORM.QueryBuilder;
using ForgeORM.SchemaOps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;

namespace ForgeORM.AspNetCore;

public static class ForgeOrmServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeOrm operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <param name="configure">The configure value.</param>
    /// <returns>The result of the AddForgeOrm operation.</returns>
    public static IServiceCollection AddForgeOrm(this IServiceCollection services, Action<ForgeOrmOptions> configure)
    {
        var options = new ForgeOrmOptions();
        configure(options);

        if (string.IsNullOrWhiteSpace(options.ConnectionString)) throw new InvalidOperationException("ForgeORM connection string is required.");
        if (options.Provider is null) throw new InvalidOperationException("ForgeORM provider is required.");
        ForgeSourceGeneratedRegistry.CompilationMode = options.CompilationMode;

        services.AddSingleton(options.Provider);
        services.AddSingleton<IForgeEntityMetadataResolver, HybridForgeEntityMetadataResolver>();
        services.AddSingleton<IForgeQueryAnalyzer, BasicForgeQueryAnalyzer>();
        services.AddSingleton<IForgeSelectQueryBuilder, ForgeORM.QueryBuilder.ForgeDynamicQueryBuilder>();
        services.AddSingleton<ForgeORM.QueryAst.IForgeDynamicQueryBuilder, ForgeORM.QueryAst.ForgeDynamicQueryBuilder>();
        services.AddScoped<IForgeArtifactManager>(sp =>
        {
            var provider = sp.GetRequiredService<IForgeDatabaseProvider>();
            return new ForgeArtifactManager(() => provider.CreateConnection(options.ConnectionString), provider);
        });
        services.AddSingleton<IForgeObjectMapper, ReflectionForgeObjectMapper>();
        services.AddSingleton<IForgeSqlIntelligence, BasicForgeSqlIntelligence>();
        services.AddMemoryCache();
        services.AddScoped<IForgeSchemaManager, ForgeSchemaManager>();
        services.AddSingleton<IForgeTraceVisualizer, LocalForgeTraceVisualizer>();
        services.AddSingleton<IForgeRequestReflector, ForgeRequestReflector>();
        services.AddSingleton<IForgeSemanticSearch, ForgeSemanticSearch>();
        services.AddScoped<ForgeDb>(sp => new ForgeDb(
            options.ConnectionString,
            sp.GetRequiredService<IForgeDatabaseProvider>(),
            sp.GetRequiredService<IForgeEntityMetadataResolver>(),
            sp.GetRequiredService<IForgeQueryAnalyzer>()));

        services.AddScoped<ForgeDbContext>(sp => new ForgeDbContext(
            options.ConnectionString,
            sp.GetRequiredService<IForgeDatabaseProvider>(),
            sp.GetRequiredService<IForgeEntityMetadataResolver>(),
            sp.GetRequiredService<IForgeQueryAnalyzer>()));

        services.AddScoped<IForgeDb>(sp => sp.GetRequiredService<ForgeDbContext>());
        return services;
    }
}
