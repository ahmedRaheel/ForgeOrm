using System.Buffers;
using System.Collections.Concurrent;
using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core.Performance;

/// <summary>
/// Central place for final-stage performance primitives: struct cache keys, generated-style executor
/// registry, SQL Server TVP helpers, enum converters, pooled buffers, and NativeAOT switches.
/// These are intentionally infrastructure pieces used by normal ForgeORM APIs rather than Fast* APIs.
/// </summary>
public static class ForgeUltimatePerformancePrimitives
{
    public static bool NativeAotMode { get; set; }

    internal static QueryHashKey CreateHashKey(
        string provider,
        Type entityType,
        string sql,
        Type? projectionType = null,
        int includeHash = 0)
        => new(provider, entityType, projectionType ?? entityType, StringComparer.Ordinal.GetHashCode(sql), includeHash);

    internal static PooledArray<T> Rent<T>(int minimumLength)
        => new(ArrayPool<T>.Shared.Rent(minimumLength));
}

internal readonly record struct QueryHashKey(
    string Provider,
    Type EntityType,
    Type ProjectionType,
    int SqlHash,
    int IncludeHash);

internal readonly struct PooledArray<T> : IDisposable
{
    public T[] Buffer { get; }
    public PooledArray(T[] buffer) => Buffer = buffer;
    public void Dispose() => ArrayPool<T>.Shared.Return(Buffer, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
}

internal static class ForgeSqlServerTvpBatching
{
    public const string DefaultIntListTypeName = "dbo.IntIdList";
    public const string DefaultLongListTypeName = "dbo.BigIntIdList";
    public const string DefaultGuidListTypeName = "dbo.GuidIdList";

    public static SqlParameter CreateIdsParameter<T>(string name, IReadOnlyCollection<T> ids, string? typeName = null)
    {
        var actualType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        var table = new DataTable();
        if (actualType == typeof(int))
        {
            table.Columns.Add("Id", typeof(int));
            foreach (var id in ids) table.Rows.Add(id);
            return new SqlParameter(name, SqlDbType.Structured) { TypeName = typeName ?? DefaultIntListTypeName, Value = table };
        }
        if (actualType == typeof(long))
        {
            table.Columns.Add("Id", typeof(long));
            foreach (var id in ids) table.Rows.Add(id);
            return new SqlParameter(name, SqlDbType.Structured) { TypeName = typeName ?? DefaultLongListTypeName, Value = table };
        }
        if (actualType == typeof(Guid))
        {
            table.Columns.Add("Id", typeof(Guid));
            foreach (var id in ids) table.Rows.Add(id);
            return new SqlParameter(name, SqlDbType.Structured) { TypeName = typeName ?? DefaultGuidListTypeName, Value = table };
        }

        throw new NotSupportedException($"TVP batching currently supports int, long and Guid keys. Type '{actualType.Name}' used expansion fallback.");
    }

    public static string CreateSqlServerTypesScript() => """
IF TYPE_ID(N'dbo.IntIdList') IS NULL CREATE TYPE dbo.IntIdList AS TABLE (Id INT NOT NULL PRIMARY KEY);
IF TYPE_ID(N'dbo.BigIntIdList') IS NULL CREATE TYPE dbo.BigIntIdList AS TABLE (Id BIGINT NOT NULL PRIMARY KEY);
IF TYPE_ID(N'dbo.GuidIdList') IS NULL CREATE TYPE dbo.GuidIdList AS TABLE (Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY);
""";
}

internal static class ForgeGeneratedEnumConverter<TEnum> where TEnum : struct, Enum
{
    private static readonly ConcurrentDictionary<string, TEnum> Names = new(StringComparer.OrdinalIgnoreCase);

    public static TEnum FromDatabase(object value)
    {
        if (value is TEnum typed) return typed;
        if (value is string text) return Names.GetOrAdd(text, static x => Enum.Parse<TEnum>(x, ignoreCase: true));
        return (TEnum)Enum.ToObject(typeof(TEnum), value);
    }

    public static object ToDatabase(TEnum value, bool storeAsNumber)
        => storeAsNumber ? Convert.ToInt64(value) : value.ToString();
}

internal static class ForgeGeneratedExecutorRegistry
{
    private static readonly ConcurrentDictionary<QueryHashKey, Delegate> Executors = new();

    public static TDelegate GetOrAdd<TDelegate>(QueryHashKey key, Func<QueryHashKey, TDelegate> factory)
        where TDelegate : Delegate
        => (TDelegate)Executors.GetOrAdd(key, k => factory(k));
}
