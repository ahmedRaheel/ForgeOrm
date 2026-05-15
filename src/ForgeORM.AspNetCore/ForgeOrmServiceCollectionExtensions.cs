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

    public void UseSqlServer(string connectionString) { ConnectionString = connectionString; Provider = new SqlServerForgeProvider(); }
    public void UsePostgreSql(string connectionString) { ConnectionString = connectionString; Provider = new PostgreSqlForgeProvider(); }
    public void UseMySql(string connectionString) { ConnectionString = connectionString; Provider = new MySqlForgeProvider(); }
    public void UseOracle(string connectionString) { ConnectionString = connectionString; Provider = new OracleForgeProvider(); }
    public void UseSqlite(string connectionString) { ConnectionString = connectionString; Provider = new SqliteForgeProvider(); }
}

public static class ForgeOrmServiceCollectionExtensions
{
    public static IServiceCollection AddForgeOrm(this IServiceCollection services, Action<ForgeOrmOptions> configure)
    {
        var options = new ForgeOrmOptions();
        configure(options);

        if (string.IsNullOrWhiteSpace(options.ConnectionString)) throw new InvalidOperationException("ForgeORM connection string is required.");
        if (options.Provider is null) throw new InvalidOperationException("ForgeORM provider is required.");

        services.AddSingleton(options.Provider);
        services.AddSingleton<IForgeEntityMetadataResolver, ReflectionForgeEntityMetadataResolver>();
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
