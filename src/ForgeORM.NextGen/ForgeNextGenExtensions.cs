using ForgeORM.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace ForgeORM.NextGen;

public static class ForgeNextGenExtensions
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="db">The db value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cache">The cache value.</param>
    /// <returns>The result of the T operation.</returns>
    public static IForgeSmartQuery<T> SmartSql<T>(this IForgeDb db, string sql, object? parameters = null, IMemoryCache? cache = null)
    {
        return new ForgeSmartQuery<T>(db, sql, parameters, cache);
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="db">The db value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="cache">The cache value.</param>
    /// <returns>The result of the T operation.</returns>
    public static IForgeSmartQuery<T> SmartSql<T>(this IForgeDb db, FormattableString sql, IMemoryCache? cache = null)
    {
        var safe = ForgeSqlSafety.From(sql);
        return new ForgeSmartQuery<T>(db, safe.Sql, safe.Parameters, cache);
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="db">The db value.</param>
    /// <returns>The result of the T operation.</returns>
    public static IForgeSmartQuery<T> ToSmartQuery<T>(this IForgeDb db)
    {
        return new ForgeSmartQuery<T>(db, $"SELECT * FROM {typeof(T).Name}");
    }

    /// <summary>
    /// Executes the TShape operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <typeparam name="TShape">The type used by the operation.</typeparam>
    /// <param name="query">The query value.</param>
    /// <returns>The result of the TShape operation.</returns>
    public static IReadOnlyList<TShape> ToShape<T, TShape>(this IForgeQuery<T> query)
    {
        // Future: source generator projection. Current MVP uses query output materialization.
        throw new NotSupportedException("ToShape over IForgeQuery requires source generator integration. Use db.SmartSql<T>().ToShape<TShape>() for runtime projection.");
    }

    /// <summary>
    /// Executes the ExecuteTransparent operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the ExecuteTransparent operation.</returns>
    public static string ExecuteTransparent(this IForgeDb db, string sql, object? parameters = null)
    {
        return parameters is null
            ? sql
            : sql + Environment.NewLine + "-- params: " + System.Text.Json.JsonSerializer.Serialize(parameters);
    }
}
