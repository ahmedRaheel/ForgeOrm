
using System.Runtime.CompilerServices;
using System.Text;
using ForgeORM.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ForgeORM.NextGen;

public static class ForgeIdeIntegrationExtensions
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="db">The db value.</param>
    /// <param name="handler">The handler value.</param>
    /// <returns>The result of the T operation.</returns>
    public static IForgeSmartQuery<T> SchemaSqlHandler<T>(this IForgeDb db, ForgeSqlInterpolatedStringHandler handler)
    {
        var sql = handler.ToSql();
        return db.SmartSql<T>(sql.Sql, sql.Parameters);
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="db">The db value.</param>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the T operation.</returns>
    public static IForgeSmartQuery<T> SchemaSql<T>(this IForgeDb db, FormattableString sql)
    {
        var safe = ForgeSqlSafety.From(sql);
        return db.SmartSql<T>(safe.Sql, safe.Parameters);
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="query">The query value.</param>
    /// <returns>The result of the T operation.</returns>
    public static IForgeSmartQuery<T> AutoJoin<T>(this IForgeSmartQuery<T> query)
    {
        // Design-time analyzers/source generators can replace this no-op with FK-aware join suggestions.
        return query;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="query">The query value.</param>
    /// <returns>The result of the T operation.</returns>
    public static IForgeSmartQuery<T> SelectAutomatic<T>(this IForgeSmartQuery<T> query)
    {
        // Future Roslyn analyzer tracks used properties and emits optimized projection SQL.
        return query;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="query">The query value.</param>
    /// <param name="visualizer">The visualizer value.</param>
    /// <returns>The result of the T operation.</returns>
    public static ForgeTraceLink TraceVisualizer<T>(this IForgeSmartQuery<T> query, IForgeTraceVisualizer visualizer)
    {
        var command = query.ExecuteTransparent();
        return visualizer.CreateTrace(command.Sql, command.Parameters, "ForgeORM");
    }
}
