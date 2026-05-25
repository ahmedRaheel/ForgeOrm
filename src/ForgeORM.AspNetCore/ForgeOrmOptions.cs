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
        ForgeSourceGeneratedRegistry.CompilationMode = mode;
    }
    /// <summary>Forces SourceGenerated-only mode for NativeAOT deployments. RuntimeEmit fallback is disabled by policy.</summary>
    public void UseNativeAotMode()
    {
        CompilationMode = ForgeOrmCompilationMode.SourceGenerated;
        ForgeSourceGeneratedRegistry.CompilationMode = ForgeOrmCompilationMode.SourceGenerated;
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
