using System.Collections.Concurrent;

namespace ForgeORM.Core;

/// <summary>
/// Global compiled query cache. The key is provider + entity/result type + stable SQL/query-AST fingerprint.
/// Stores final SQL plus the parameter writer shape so all normal methods reuse the same plan.
/// </summary>
public static class ForgeCompiledQueryCache
{
    private static readonly ConcurrentDictionary<ForgeCompiledQueryKey, ForgeCompiledQueryPlan> Cache = new();

    public static ForgeCompiledQueryPlan GetOrAdd(
        string provider,
        Type resultType,
        string sqlOrAst,
        Type? parameterType,
        Func<ForgeCompiledQueryPlan> factory)
    {
        var key = new ForgeCompiledQueryKey(provider, resultType.FullName ?? resultType.Name, parameterType?.FullName, Fingerprint(sqlOrAst));
        return Cache.GetOrAdd(key, _ => factory());
    }

    public static string Fingerprint(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return ForgeFastHash.FingerprintSql(value);
    }
}

public readonly record struct ForgeCompiledQueryKey(
    string Provider,
    string ResultType,
    string? ParameterType,
    string QueryFingerprint);

public sealed record ForgeCompiledQueryPlan(
    string Sql,
    Type ResultType,
    Type? ParameterType,
    string Provider,
    string QueryFingerprint);
