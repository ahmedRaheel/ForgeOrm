
using System.Runtime.CompilerServices;
using System.Text;
using ForgeORM.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ForgeORM.NextGen;

[AttributeUsage(AttributeTargets.Class)]
public sealed class DbSchemaMatchAttribute : Attribute
{
    public string? TableName { get; init; }
    public bool FailOnDrift { get; init; } = true;
}

public sealed class ForgeSchemaAwareSql
{
    public required string Sql { get; init; }
    public required Dictionary<string, object?> Parameters { get; init; }
    public IReadOnlyList<string> ReferencedTables { get; init; } = [];
    public IReadOnlyList<string> ReferencedColumns { get; init; } = [];
}

[InterpolatedStringHandler]
public ref struct ForgeSqlInterpolatedStringHandler
{
    private StringBuilder _sql;
    private Dictionary<string, object?> _parameters;
    private int _index;

    /// <summary>
    /// Executes the ForgeSqlInterpolatedStringHandler operation.
    /// </summary>
    /// <param name="literalLength">The literalLength value.</param>
    /// <param name="formattedCount">The formattedCount value.</param>
    /// <returns>The result of the ForgeSqlInterpolatedStringHandler operation.</returns>
    public ForgeSqlInterpolatedStringHandler(int literalLength, int formattedCount)
    {
        _sql = new StringBuilder(literalLength + formattedCount * 8);
        _parameters = new Dictionary<string, object?>(formattedCount);
        _index = 0;
    }

    /// <summary>
    /// Executes the AppendLiteral operation.
    /// </summary>
    /// <param name="value">The value value.</param>
    public void AppendLiteral(string value) => _sql.Append(value);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="value">The value value.</param>
    /// <returns>The result of the T operation.</returns>
    public void AppendFormatted<T>(T value)
    {
        var name = "p" + _index++;
        _sql.Append('@').Append(name);
        _parameters[name] = value;
    }

    /// <summary>
    /// Executes the ToSql operation.
    /// </summary>
    /// <returns>The result of the ToSql operation.</returns>
    public ForgeSchemaAwareSql ToSql()
    {
        return new ForgeSchemaAwareSql
        {
            Sql = _sql.ToString(),
            Parameters = _parameters
        };
    }
}

public interface IForgeTraceVisualizer
/// <summary>
/// Defines the CreateTrace operation.
/// </summary>
/// <param name="sql">The sql value.</param>
/// <param name="parameters">The parameters value.</param>
/// <param name="providerName">The providerName value.</param>
/// <returns>The result of the CreateTrace operation.</returns>
{
    /// <summary>
    /// Defines the CreateTrace operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="providerName">The providerName value.</param>
    /// <returns>The result of the CreateTrace operation.</returns>
    ForgeTraceLink CreateTrace(string sql, object? parameters, string providerName);
}

public sealed class ForgeTraceLink
{
    public required string TraceId { get; init; }
    public required string LocalUrl { get; init; }
    public required string Sql { get; init; }
    public object? Parameters { get; init; }
    public string ProviderName { get; init; } = "Unknown";
    public IReadOnlyList<string> HotPaths { get; init; } = [];
}

public sealed class LocalForgeTraceVisualizer : IForgeTraceVisualizer
{
    /// <summary>
    /// Executes the CreateTrace operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="providerName">The providerName value.</param>
    /// <returns>The result of the CreateTrace operation.</returns>
    public ForgeTraceLink CreateTrace(string sql, object? parameters, string providerName)
    {
        var id = Guid.NewGuid().ToString("N");
        return new ForgeTraceLink
        {
            TraceId = id,
            LocalUrl = "http://localhost:5055/forge-trace/" + id,
            Sql = sql,
            Parameters = parameters,
            ProviderName = providerName,
            HotPaths = sql.Contains("SELECT *", StringComparison.OrdinalIgnoreCase)
                ? ["SELECT * may cause unnecessary IO and mapping overhead."]
                : []
        };
    }
}

public interface IForgeRequestReflector
/// <summary>
/// Defines the T operation.
/// </summary>
/// <typeparam name="T">The type used by the operation.</typeparam>
/// <param name="context">The context value.</param>
/// <returns>The result of the T operation.</returns>
{
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="context">The context value.</param>
    /// <returns>The result of the T operation.</returns>
    ForgeBuiltQuery ReflectRequest<T>(HttpContext context);
}

public sealed class ForgeRequestReflector : IForgeRequestReflector
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="context">The context value.</param>
    /// <returns>The result of the T operation.</returns>
    public ForgeBuiltQuery ReflectRequest<T>(HttpContext context)
    {
        var table = typeof(T).Name;
        var conditions = new List<string>();
        var parameters = new Dictionary<string, object?>();

        foreach (var query in context.Request.Query)
        {
            var key = query.Key;
            if (key.Equals("sort", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("page", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("pageSize", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var param = "p_" + key;
            conditions.Add($"{key} = @{param}");
            parameters[param] = query.Value.ToString();
        }

        var sql = $"SELECT * FROM {table}";
        if (conditions.Count > 0)
            sql += " WHERE " + string.Join(" AND ", conditions);

        if (context.Request.Query.TryGetValue("sort", out var sort))
            sql += " ORDER BY " + sort;

        return new ForgeBuiltQuery { Sql = sql, Parameters = parameters };
    }
}

public interface IForgeSemanticSearch
/// <summary>
/// Defines the T operation.
/// </summary>
/// <typeparam name="T">The type used by the operation.</typeparam>
/// <param name="propertyOrColumn">The propertyOrColumn value.</param>
/// <param name="searchText">The searchText value.</param>
/// <param name="top">The top value.</param>
/// <returns>The result of the T operation.</returns>
{
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="propertyOrColumn">The propertyOrColumn value.</param>
    /// <param name="searchText">The searchText value.</param>
    /// <param name="top">The top value.</param>
    /// <returns>The result of the T operation.</returns>
    ForgeBuiltQuery SearchSemantic<T>(string propertyOrColumn, string searchText, int top = 20);
}

public sealed class ForgeSemanticSearch : IForgeSemanticSearch
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="propertyOrColumn">The propertyOrColumn value.</param>
    /// <param name="searchText">The searchText value.</param>
    /// <param name="top">The top value.</param>
    /// <returns>The result of the T operation.</returns>
    public ForgeBuiltQuery SearchSemantic<T>(string propertyOrColumn, string searchText, int top = 20)
    {
        // Provider-specific implementations can replace this with pgvector, SQL Server vector search,
        // Azure AI Search, or custom embedding functions.
        var table = typeof(T).Name;
        var sql = $"SELECT TOP ({top}) * FROM {table} WHERE {propertyOrColumn} LIKE @SearchText";
        return new ForgeBuiltQuery
        {
            Sql = sql,
            Parameters = new { SearchText = "%" + searchText + "%" }
        };
    }
}

public static class ForgeIdeIntegrationExtensions
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="db">The db value.</param>
    /// <param name="handler">The handler value.</param>
    /// <returns>The result of the T operation.</returns>
    public static IForgeSmartQuery<T> SchemaSqlHandler<T>(this IForgeDb db, ForgeSqlInterpolatedStringHandler handler)
    {
        var sql = handler.ToSql();
        return db.SmartSql<T>(sql.Sql, sql.Parameters);
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="db">The db value.</param>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the T operation.</returns>
    public static IForgeSmartQuery<T> SchemaSql<T>(this IForgeDb db, FormattableString sql)
    {
        var safe = ForgeSqlSafety.From(sql);
        return db.SmartSql<T>(safe.Sql, safe.Parameters);
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="query">The query value.</param>
    /// <returns>The result of the T operation.</returns>
    public static IForgeSmartQuery<T> AutoJoin<T>(this IForgeSmartQuery<T> query)
    {
        // Design-time analyzers/source generators can replace this no-op with FK-aware join suggestions.
        return query;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="query">The query value.</param>
    /// <returns>The result of the T operation.</returns>
    public static IForgeSmartQuery<T> SelectAutomatic<T>(this IForgeSmartQuery<T> query)
    {
        // Future Roslyn analyzer tracks used properties and emits optimized projection SQL.
        return query;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="query">The query value.</param>
    /// <param name="visualizer">The visualizer value.</param>
    /// <returns>The result of the T operation.</returns>
    public static ForgeTraceLink TraceVisualizer<T>(this IForgeSmartQuery<T> query, IForgeTraceVisualizer visualizer)
    {
        var command = query.ExecuteTransparent();
        return visualizer.CreateTrace(command.Sql, command.Parameters, "ForgeORM");
    }
}
