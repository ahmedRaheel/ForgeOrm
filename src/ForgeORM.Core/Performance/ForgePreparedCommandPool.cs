using System.Collections.Concurrent;

namespace ForgeORM.Core.Performance;

internal sealed record ForgePreparedCommandTemplate(
    string Sql,
    string[] ParameterNames,
    Type? ParameterType,
    int? TimeoutSeconds);

internal static class ForgePreparedCommandPool
{
    private static readonly ConcurrentDictionary<string, ForgePreparedCommandTemplate> Templates = new(StringComparer.Ordinal);

    public static ForgePreparedCommandTemplate GetOrAdd(string sql, string[] parameterNames, Type? parameterType, int? timeoutSeconds)
    {
        var key = string.Join('|', sql, parameterType?.FullName ?? string.Empty, timeoutSeconds?.ToString() ?? string.Empty, string.Join(',', parameterNames));
        return Templates.GetOrAdd(key, _ => new ForgePreparedCommandTemplate(sql, parameterNames, parameterType, timeoutSeconds));
    }
}
