using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;

namespace ForgeORM.Core;

/// <summary>
/// Caches immutable command shape metadata. DbCommand instances are provider/connection scoped and must not be reused,
/// but SQL shape, parameter names, command type and provider keys are safe to cache and reuse across executions.
/// </summary>
internal static class ForgePerformanceCommandPlanCache
{
    private static readonly ConcurrentDictionary<ForgePerformanceCommandPlanKey, ForgePerformanceCommandPlan> Cache = new();

    public static ForgePerformanceCommandPlan GetOrAdd(string providerName, string sql, CommandType commandType, Type? parameterType)
    {
        var key = new ForgePerformanceCommandPlanKey(providerName, commandType, parameterType?.FullName ?? "<none>", Fingerprint(sql));
        return Cache.GetOrAdd(key, _ => new ForgePerformanceCommandPlan(providerName, sql, commandType, parameterType, ExtractParameterNames(sql)));
    }

    public static DbCommand CreateCommand(DbConnection connection, ForgePerformanceCommandPlan plan, DbTransaction? transaction, int? timeoutSeconds)
    {
        var command = connection.CreateCommand();
        command.CommandText = plan.Sql;
        command.CommandType = plan.CommandType;

        if (transaction is not null)
            command.Transaction = transaction;

        if (timeoutSeconds.HasValue)
            command.CommandTimeout = timeoutSeconds.Value;

        return command;
    }

    public static string Fingerprint(string sql)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sql));
        return Convert.ToHexString(bytes);
    }

    private static string[] ExtractParameterNames(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return Array.Empty<string>();

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < sql.Length - 1; i++)
        {
            if (sql[i] is not '@' and not ':')
                continue;

            var start = i + 1;
            if (start >= sql.Length || !(char.IsLetter(sql[start]) || sql[start] == '_'))
                continue;

            var end = start + 1;
            while (end < sql.Length && (char.IsLetterOrDigit(sql[end]) || sql[end] == '_'))
                end++;

            names.Add(sql[start..end]);
            i = end;
        }

        return names.ToArray();
    }
}

internal readonly record struct ForgePerformanceCommandPlanKey(string ProviderName, CommandType CommandType, string ParameterType, string SqlFingerprint);

internal sealed record ForgePerformanceCommandPlan(string ProviderName, string Sql, CommandType CommandType, Type? ParameterType, string[] ParameterNames);
