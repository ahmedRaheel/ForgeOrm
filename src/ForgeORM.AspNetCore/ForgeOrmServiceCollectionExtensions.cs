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

public sealed class ForgeOrmOptions
{
    internal string? ConnectionString { get; private set; }
    internal IForgeDatabaseProvider? Provider { get; private set; }
    internal ForgeOrmCompilationMode CompilationMode { get; private set; } = ForgeOrmCompilationMode.Auto;

    /// <summary>
    /// Executes the UseSqlServer operation.
    /// </summary>
    /// <param name="ConnectionString">The ConnectionString value.</param>
    public void UseSqlServer(string connectionString) { ConnectionString = connectionString; Provider = new SqlServerForgeProvider(); }

    /// <summary>Configures compilation mode. Source-generation modes are intentionally mapped to RuntimeEmit because this package is RuntimeEmit-only.</summary>
    public void UseCompilationMode(ForgeOrmCompilationMode mode)
    {
        CompilationMode = ForgeOrmCompilationMode.RuntimeEmit;
        ForgeSourceGeneratedRegistry.CompilationMode = ForgeOrmCompilationMode.RuntimeEmit;
    }
    /// <summary>NativeAOT source-generation mode is not enabled in the RuntimeEmit-only package.</summary>
    public void UseNativeAotMode()
    {
        CompilationMode = ForgeOrmCompilationMode.RuntimeEmit;
        ForgeSourceGeneratedRegistry.CompilationMode = ForgeOrmCompilationMode.RuntimeEmit;
        ForgeORM.Core.Performance.ForgeUltimatePerformancePrimitives.NativeAotMode = false;
    }

    /// <summary>
    /// Executes the UsePostgreSql operation.
    /// </summary>
    /// <param name="ConnectionString">The ConnectionString value.</param>
    public void UsePostgreSql(string connectionString) { ConnectionString = connectionString; Provider = new PostgreSqlForgeProvider(); }
    /// <summary>
    /// Executes the UseMySql operation.
    /// </summary>
    /// <param name="ConnectionString">The ConnectionString value.</param>
    public void UseMySql(string connectionString) { ConnectionString = connectionString; Provider = new MySqlForgeProvider(); }
    /// <summary>
    /// Executes the UseOracle operation.
    /// </summary>
    /// <param name="ConnectionString">The ConnectionString value.</param>
    public void UseOracle(string connectionString) { ConnectionString = connectionString; Provider = new OracleForgeProvider(); }
    /// <summary>
    /// Executes the UseSqlite operation.
    /// </summary>
    /// <param name="ConnectionString">The ConnectionString value.</param>
    public void UseSqlite(string connectionString) { ConnectionString = connectionString; Provider = new SqliteForgeProvider(); }
}

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
        ForgeSourceGeneratedRegistry.CompilationMode = ForgeOrmCompilationMode.RuntimeEmit;

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
