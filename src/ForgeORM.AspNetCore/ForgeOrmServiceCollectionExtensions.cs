using System.Reflection;
using System.Threading;
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

    /// <summary>Configures whether ForgeORM uses source-generated accessors, RuntimeEmit MSIL, or Auto mode.</summary>
    public void UseCompilationMode(ForgeOrmCompilationMode mode)
    {
        CompilationMode = mode;
        ForgeOrmSourceGenerationBootstrap.Configure(mode);
    }
    /// <summary>Forces SourceGenerated-only mode for NativeAOT deployments. RuntimeEmit fallback is disabled by policy.</summary>
    public void UseNativeAotMode()
    {
        CompilationMode = ForgeOrmCompilationMode.SourceGenerated;
        ForgeOrmSourceGenerationBootstrap.Configure(ForgeOrmCompilationMode.SourceGenerated);
        ForgeORM.Core.Performance.ForgeUltimatePerformancePrimitives.NativeAotMode = true;
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
        ForgeOrmSourceGenerationBootstrap.Configure(options.CompilationMode);

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


internal static class ForgeOrmSourceGenerationBootstrap
{
    private static int _configured;

    /// <summary>
    /// Applies the selected compilation mode globally and primes source-generated provider discovery.
    /// Important: this does not require any user-side registry call. With the NuGet package,
    /// ForgeORM.AspNetCore carries the analyzer so generated providers are created at build time.
    /// At runtime this method only selects the generated path and discovers/registers already
    /// generated providers from loaded assemblies.
    /// </summary>
    public static void Configure(ForgeOrmCompilationMode mode)
    {
        ForgeSourceGeneratedRegistry.CompilationMode = mode;

        if (mode == ForgeOrmCompilationMode.SourceGenerated ||
            mode == ForgeOrmCompilationMode.SourceGeneratedStrict ||
            mode == ForgeOrmCompilationMode.Auto)
        {
            ForgeSourceGeneratedRegistry.DiscoverGeneratedProvidersFromLoadedAssemblies();
        }

        if (Interlocked.Exchange(ref _configured, 1) == 0)
        {
            AppDomain.CurrentDomain.AssemblyLoad += (_, args) =>
            {
                var current = ForgeSourceGeneratedRegistry.CompilationMode;
                if (current == ForgeOrmCompilationMode.RuntimeEmit)
                    return;

                ForgeSourceGeneratedRegistry.DiscoverGeneratedProviders(args.LoadedAssembly);
            };
        }
    }
}
