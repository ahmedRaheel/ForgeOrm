using System.Collections.Concurrent;
using System.Text.Json;
using ForgeORM.Abstractions;
using AbstractionTenantContext = ForgeORM.Abstractions.ForgeTenantContext;
using AbstractionOutboxMessage = ForgeORM.Abstractions.ForgeOutboxMessage;

namespace ForgeORM.Core;

public sealed class ForgeReportingEngine : IForgeReportingEngine
{
    /// <summary>
    /// Executes the Build operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the Build operation.</returns>
    public ForgeReportSql Build(ForgeReportRequest request, string provider = "SqlServer")
    {
        if (string.IsNullOrWhiteSpace(request.From))
            throw new ArgumentException("Report source table/query is required.", nameof(request));

        var columns = request.Columns.Count == 0
            ? "*"
            : string.Join(", ", request.Columns.Select(c => string.IsNullOrWhiteSpace(c.Alias)
                ? c.Expression
                : $"{c.Expression} AS {c.Alias}"));

        var sql = $"SELECT {columns} FROM {request.From}";
        if (request.Filters.Count > 0)
            sql += " WHERE " + string.Join(" AND ", request.Filters.Select(x => x.Expression));

        if (!string.IsNullOrWhiteSpace(request.GroupBy))
            sql += " GROUP BY " + request.GroupBy;

        if (!string.IsNullOrWhiteSpace(request.OrderBy))
            sql += " ORDER BY " + request.OrderBy;

        if (request.Take.HasValue)
        {
            sql += provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
                ? $" OFFSET {request.Skip ?? 0} ROWS FETCH NEXT {request.Take.Value} ROWS ONLY"
                : $" LIMIT {request.Take.Value} OFFSET {request.Skip ?? 0}";
        }

        var parameters = request.Filters
            .Where(x => x.Parameters is not null)
            .Select(x => x.Parameters)
            .ToList();

        return new ForgeReportSql(sql, parameters.Count == 0 ? null : parameters);
    }
}
