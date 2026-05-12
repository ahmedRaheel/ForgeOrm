using ForgeORM.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace ForgeORM.NextGen;

public static class ForgeNextGenExtensions
{
    public static IForgeSmartQuery<T> SmartSql<T>(this IForgeDb db, string sql, object? parameters = null, IMemoryCache? cache = null)
    {
        return new ForgeSmartQuery<T>(db, sql, parameters, cache);
    }

    public static IForgeSmartQuery<T> SmartSql<T>(this IForgeDb db, FormattableString sql, IMemoryCache? cache = null)
    {
        var safe = ForgeSqlSafety.From(sql);
        return new ForgeSmartQuery<T>(db, safe.Sql, safe.Parameters, cache);
    }

    public static IForgeSmartQuery<T> ToSmartQuery<T>(this IForgeDb db)
    {
        return new ForgeSmartQuery<T>(db, $"SELECT * FROM {typeof(T).Name}");
    }

    public static IReadOnlyList<TShape> ToShape<T, TShape>(this IForgeQuery<T> query)
    {
        // Future: source generator projection. Current MVP uses query output materialization.
        throw new NotSupportedException("ToShape over IForgeQuery requires source generator integration. Use db.SmartSql<T>().ToShape<TShape>() for runtime projection.");
    }

    public static string ExecuteTransparent(this IForgeDb db, string sql, object? parameters = null)
    {
        return parameters is null
            ? sql
            : sql + Environment.NewLine + "-- params: " + System.Text.Json.JsonSerializer.Serialize(parameters);
    }
}
