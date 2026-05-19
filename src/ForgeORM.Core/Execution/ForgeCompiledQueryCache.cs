using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

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

        var normalized = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
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
