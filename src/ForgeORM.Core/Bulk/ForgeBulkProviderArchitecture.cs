using ForgeORM.Abstractions;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;

namespace ForgeORM.Core;

/// <summary>
/// Provider-neutral bulk provider contract. Core exposes only generic operations;
/// provider projects decide whether to use TVP, COPY, multi-row batching, array binding, MERGE, or fallback SQL.
/// </summary>
public interface IForgeBulkProvider
{
    string ProviderName { get; }

    ValueTask<int> InsertBulkAsync<T>(DbConnection connection, string tableName, IReadOnlyList<T> rows, ForgeProviderBulkOptions options, CancellationToken cancellationToken = default);

    ValueTask<int> UpdateBulkAsync<T>(DbConnection connection, string tableName, IReadOnlyList<T> rows, string keyColumn = "Id", ForgeProviderBulkOptions? bulkOptions = null, CancellationToken cancellationToken = default);

    ValueTask<int> DeleteBulkAsync<TKey>(DbConnection connection, string tableName, IReadOnlyList<TKey> keys, string keyColumn = "Id", CancellationToken cancellationToken = default);

    ValueTask<int> GraphUpdateAsync<T>(DbConnection connection, string tableName, IReadOnlyList<T> rows, string keyColumn = "Id", CancellationToken cancellationToken = default);
}

/// <summary>
/// Provider-neutral cached bulk metadata. It deliberately contains no SQL Server-specific types.
/// Provider implementations translate this plan into TVP, COPY, multi-row insert, or array binding.
/// </summary>
public sealed class ForgeBulkEntityPlan
{
    public ForgeBulkEntityPlan(
        Type entityType,
        string tableName,
        string keyColumn,
        IReadOnlyList<PropertyInfo> columns)
    {
        EntityType = entityType;
        TableName = tableName;
        KeyColumn = keyColumn;
        Columns = columns;
    }

    public Type EntityType { get; }

    public string TableName { get; }

    public string KeyColumn { get; }

    public IReadOnlyList<PropertyInfo> Columns { get; }
}

/// <summary>
/// One bulk plan per entity type. SQL text/provider handles are produced by the provider layer,
/// not by core, so the same public API works across SQL Server, PostgreSQL, MySQL, and Oracle.
/// </summary>
public static class ForgeBulkPlanCache<T>
{
    public static ForgeBulkEntityPlan GetOrCreate(string tableName, string keyColumn = "Id")
        => Cache.GetOrAdd(tableName + "|" + keyColumn, _ => Build(tableName, keyColumn));

    private static readonly ConcurrentDictionary<string, ForgeBulkEntityPlan> Cache = new(StringComparer.OrdinalIgnoreCase);

    private static ForgeBulkEntityPlan Build(string tableName, string keyColumn)
    {
        var properties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && IsScalar(p.PropertyType))
            .ToArray();

        return new ForgeBulkEntityPlan(typeof(T), tableName, keyColumn, properties);
    }

    private static bool IsScalar(Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;

        return actual.IsPrimitive
               || actual.IsEnum
               || actual == typeof(string)
               || actual == typeof(Guid)
               || actual == typeof(decimal)
               || actual == typeof(DateTime)
               || actual == typeof(DateTimeOffset)
               || actual == typeof(DateOnly)
               || actual == typeof(TimeOnly)
               || actual == typeof(TimeSpan)
               || actual == typeof(byte[]);
    }
}
