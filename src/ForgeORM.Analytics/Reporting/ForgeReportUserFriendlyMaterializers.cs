using System.Linq.Expressions;
using ForgeORM.Core;
using ForgeORM.Core.Materialization;

namespace ForgeORM.Analytics.Reporting;

/// <summary>
/// User-friendly terminal methods for reporting. Reports should not force users to manually render SQL and execute it.
/// </summary>
public static class ForgeReportUserFriendlyMaterializers
{
    /// <summary>
    /// Renders report SQL and executes it as dynamic dictionary rows.
    /// </summary>
    public static async ValueTask<IReadOnlyList<Dictionary<string, object?>>> ToDictionaryAsync<TReport>(
        this TReport report,
        CancellationToken cancellationToken = default)
        where TReport : class
    {
        var db = TryGetDb(report);
        var sql = RenderSql(report);
        var parameters = TryGetParameters(report);

        if (db is null)
        {
            throw new InvalidOperationException(
                "This report builder does not expose a ForgeDb instance. Use the builder returned by db.Report<T>(...) or add a Db property to the report builder.");
        }

        return await db.QueryDictionaryAsync(sql, parameters, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Renders report SQL, executes it and returns JSON-friendly rows with metadata.
    /// </summary>
    public static async ValueTask<ForgeJsonProjection> ToJsonAsync<TReport>(
        this TReport report,
        CancellationToken cancellationToken = default)
        where TReport : class
    {
        var db = TryGetDb(report);
        var sql = RenderSql(report);
        var parameters = TryGetParameters(report);

        if (db is null)
        {
            throw new InvalidOperationException(
                "This report builder does not expose a ForgeDb instance. Use the builder returned by db.Report<T>(...) or add a Db property to the report builder.");
        }

        return await db.QueryJsonProjectionAsync(
            sql,
            parameters,
            name: TryGetReportName(report),
            cancellationToken);
    }

    /// <summary>
    /// Renders report SQL, executes it and returns a DataFrame-friendly table.
    /// </summary>
    public static async ValueTask<ForgeTabularResult> ToDataFrameAsync<TReport>(
        this TReport report,
        CancellationToken cancellationToken = default)
        where TReport : class
    {
        var db = TryGetDb(report);
        var sql = RenderSql(report);
        var parameters = TryGetParameters(report);

        if (db is null)
        {
            throw new InvalidOperationException(
                "This report builder does not expose a ForgeDb instance. Use the builder returned by db.Report<T>(...) or add a Db property to the report builder.");
        }

        return await db.QueryDataFrameAsync(
            sql,
            parameters,
            name: TryGetReportName(report),
            cancellationToken);
    }

    /// <summary>
    /// Renders report SQL, executes it and returns CSV text.
    /// </summary>
    public static async ValueTask<string> ToCsvAsync<TReport>(
        this TReport report,
        CancellationToken cancellationToken = default)
        where TReport : class
    {
        var rows = await report.ToDictionaryAsync(cancellationToken);
        return ForgeMaterializationSerializer.ToCsv(rows);
    }

    /// <summary>
    /// Renders report SQL, executes it and maps fixed-shape results to DTOs.
    /// Use this only when columns match the DTO. Dynamic pivots should use ToDictionaryAsync / ToJsonAsync.
    /// </summary>
    public static async ValueTask<IReadOnlyList<TDto>> ToDtoListAsync<TReport, TDto>(
        this TReport report,
        CancellationToken cancellationToken = default)
        where TReport : class
    {
        var db = TryGetDb(report);
        var sql = RenderSql(report);
        var parameters = TryGetParameters(report);

        if (db is null)
        {
            throw new InvalidOperationException(
                "This report builder does not expose a ForgeDb instance. Use the builder returned by db.Report<T>(...) or add a Db property to the report builder.");
        }

        var rows = await db.QueryAsync<TDto>(sql, parameters, cancellationToken: cancellationToken);
        return rows.ToList();
    }

    /// <summary>
    /// Returns SQL only. This is preview/debug behavior, not the default execution path.
    /// </summary>
    public static string ToSqlProjection<TReport>(
        this TReport report)
        where TReport : class
    {
        return RenderSql(report);
    }

    private static string RenderSql(object report)
    {
        var type = report.GetType();

        foreach (var methodName in new[] { "ToSql", "ToJsonProjection", "Render", "BuildSql" })
        {
            var method = type.GetMethod(methodName, Type.EmptyTypes);
            if (method is null)
            {
                continue;
            }

            var value = method.Invoke(report, null);

            if (value is string sql)
            {
                return sql;
            }

            var sqlProperty = value?.GetType().GetProperty("Sql");
            var sqlValue = sqlProperty?.GetValue(value)?.ToString();

            if (!string.IsNullOrWhiteSpace(sqlValue))
            {
                return sqlValue!;
            }
        }

        throw new InvalidOperationException(
            $"Report builder '{type.Name}' does not expose ToSql(), ToJsonProjection(), Render() or BuildSql().");
    }

    private static object? TryGetParameters(object report)
    {
        var type = report.GetType();

        foreach (var propertyName in new[] { "Parameters", "ReportParameters" })
        {
            var property = type.GetProperty(propertyName);
            if (property is not null)
            {
                return property.GetValue(report);
            }
        }

        var definition = type.GetProperty("Definition")?.GetValue(report)
            ?? type.GetField("_definition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(report);

        return definition?.GetType().GetProperty("Parameters")?.GetValue(definition);
    }

    private static string? TryGetReportName(object report)
    {
        var type = report.GetType();

        foreach (var propertyName in new[] { "Name", "ReportName" })
        {
            var property = type.GetProperty(propertyName);
            var value = property?.GetValue(report)?.ToString();

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        var definition = type.GetProperty("Definition")?.GetValue(report)
            ?? type.GetField("_definition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(report);

        return definition?.GetType().GetProperty("Name")?.GetValue(definition)?.ToString();
    }

    private static ForgeDb? TryGetDb(object report)
    {
        var type = report.GetType();

        foreach (var propertyName in new[] { "Db", "Database", "Context" })
        {
            var property = type.GetProperty(propertyName);
            if (property?.GetValue(report) is ForgeDb db)
            {
                return db;
            }
        }

        foreach (var fieldName in new[] { "_db", "_database", "_context" })
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field?.GetValue(report) is ForgeDb db)
            {
                return db;
            }
        }

        return null;
    }
}
