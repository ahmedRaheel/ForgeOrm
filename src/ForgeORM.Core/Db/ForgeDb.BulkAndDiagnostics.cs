using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public partial class ForgeDb : IForgeBulkOperations, IForgeDiagnostics
{
    ForgeQueryAnalysis IForgeDiagnostics.Analyze(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        var normalized = sql.Trim();
        var analysis = new ForgeQueryAnalysis();

        if (normalized.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase)
            && !normalized.Contains(" WHERE ", StringComparison.OrdinalIgnoreCase))
        {
            analysis.Errors.Add("DELETE statement without WHERE clause detected.");
        }

        if (normalized.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)
            && !normalized.Contains(" WHERE ", StringComparison.OrdinalIgnoreCase))
        {
            analysis.Errors.Add("UPDATE statement without WHERE clause detected.");
        }

        if (normalized.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            && !normalized.Contains(" WHERE ", StringComparison.OrdinalIgnoreCase))
        {
            analysis.Warnings.Add("SELECT query does not contain a WHERE clause.");
            analysis.Suggestions.Add("Add a WHERE clause when querying large tables.");
        }

        if (normalized.Contains("SELECT *", StringComparison.OrdinalIgnoreCase))
        {
            analysis.Warnings.Add("SELECT * detected.");
            analysis.Suggestions.Add("Select only required columns to reduce IO and materialization cost.");
        }

        if (normalized.Contains(" JOIN ", StringComparison.OrdinalIgnoreCase))
            analysis.Suggestions.Add("Ensure join columns are indexed.");

        if (normalized.Contains("LIKE '%", StringComparison.OrdinalIgnoreCase))
        {
            analysis.Warnings.Add("Leading wildcard LIKE detected.");
            analysis.Suggestions.Add("Leading wildcard searches usually cannot use normal indexes efficiently.");
        }

        if (normalized.Contains(" ORDER BY ", StringComparison.OrdinalIgnoreCase)
            && !normalized.Contains(" OFFSET ", StringComparison.OrdinalIgnoreCase)
            && !normalized.Contains(" TOP ", StringComparison.OrdinalIgnoreCase)
            && !normalized.Contains(" LIMIT ", StringComparison.OrdinalIgnoreCase))
        {
            analysis.Suggestions.Add("For large ORDER BY queries, use matching indexes and paging.");
        }

        return analysis;
    }

    async ValueTask IForgeBulkOperations.BulkDeleteAsync<T>(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken)
    {
        var metadata = _metadata.Resolve<T>();

        await ((IForgeBulkOperations)this)
            .BulkDeleteAsync(metadata.TableName, ids, metadata.KeyColumn, cancellationToken)
            .ConfigureAwait(false);
    }

    async ValueTask IForgeBulkOperations.BulkDeleteAsync(
        string tableName,
        IReadOnlyCollection<int> ids,
        string keyColumn,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyColumn);

        if (ids is null || ids.Count == 0)
            return;

        var distinctIds = ids.Distinct().ToArray();
        if (distinctIds.Length == 0)
            return;

        var parameters = new Dictionary<string, object?>(distinctIds.Length, StringComparer.OrdinalIgnoreCase);
        var parameterNames = new string[distinctIds.Length];

        for (var i = 0; i < distinctIds.Length; i++)
        {
            var name = "Id" + i;
            parameterNames[i] = "@" + name;
            parameters[name] = distinctIds[i];
        }

        var sql = $"DELETE FROM {tableName} WHERE {keyColumn} IN ({string.Join(", ", parameterNames)})";

        await ExecuteAsync(sql, parameters, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
