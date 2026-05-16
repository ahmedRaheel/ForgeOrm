using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using ForgeORM.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace ForgeORM.NextGen;

public sealed class ForgeExplainResult
{
    public required string Sql { get; init; }
    public string ProviderName { get; init; } = "Unknown";
    public string? RawPlan { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public IReadOnlyList<string> Suggestions { get; init; } = [];
}

public sealed class ForgeTransparentCommand
{
    public required string Sql { get; init; }
    public object? Parameters { get; init; }
    public string DebugView => Parameters is null ? Sql : $"{Sql}\n-- params: {JsonSerializer.Serialize(Parameters)}";

    /// <summary>
    /// Executes the Execute operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    /// <returns>The result of the Execute operation.</returns>
    public int Execute(IForgeDb db) => db.Execute(Sql, Parameters);
    /// <summary>
    /// Executes the ExecuteAsync operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ExecuteAsync operation.</returns>
    public Task<int> ExecuteAsync(IForgeDb db, CancellationToken cancellationToken = default)
        => db.ExecuteAsync(Sql, Parameters, cancellationToken: cancellationToken);
}

public sealed class ForgeCacheOptions
{
    public TimeSpan Duration { get; init; }
    public string? Key { get; init; }
    public bool UseMemoryCache { get; init; } = true;
}

public sealed class ForgeResiliencePolicy
{
    /// <summary>
    /// Executes the FromMilliseconds operation.
    /// </summary>
    /// <returns>The result of the FromMilliseconds operation.</returns>
    public int RetryCount { get; init; } = 0;
    /// <summary>
    /// Executes the FromMilliseconds operation.
    /// </summary>
    /// <returns>The result of the FromMilliseconds operation.</returns>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMilliseconds(100);
    /// <summary>
    /// Executes the FromSeconds operation.
    /// </summary>
    /// <returns>The result of the FromSeconds operation.</returns>
    public bool UseCircuitBreaker { get; init; }
    /// <summary>
    /// Executes the FromSeconds operation.
    /// </summary>
    /// <returns>The result of the FromSeconds operation.</returns>
    public TimeSpan CircuitBreakDuration { get; init; } = TimeSpan.FromSeconds(30);
}

public sealed class ForgeSchemaDiff
{
    public bool HasChanges => Changes.Count > 0;
    public IReadOnlyList<string> Changes { get; init; } = [];
    public string FixScript { get; init; } = string.Empty;
}

public sealed class ForgeSchemaVerificationResult
{
    public bool IsValid => Errors.Count == 0;
    public IReadOnlyList<string> Errors { get; init; } = [];
    public string FixScript { get; init; } = string.Empty;
}

public sealed class ForgeMockDataSet
{
    private readonly Dictionary<Type, IList> _tables = [];

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the T operation.</returns>
    public ForgeMockDataSet Add<T>(IEnumerable<T> rows)
    {
        _tables[typeof(T)] = rows.ToList();
        return this;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    public IReadOnlyList<T> Get<T>()
    {
        if (_tables.TryGetValue(typeof(T), out var rows))
            return rows.Cast<T>().ToList();

        return [];
    }
}

public sealed class ForgeShadowRow<T>
{
    /// <summary>
    /// Executes the ShadowProperty operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the ShadowProperty operation.</returns>
    public required T Entity { get; init; }
    /// <summary>
    /// Executes the ShadowProperty operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the ShadowProperty operation.</returns>
    public Dictionary<string, object?> ShadowValues { get; init; } = [];
    /// <summary>
    /// Executes the ShadowProperty operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the ShadowProperty operation.</returns>
    public object? ShadowProperty(string name) => ShadowValues.TryGetValue(name, out var value) ? value : null;
}

public interface IForgeSchemaManager
/// <summary>
/// Defines the T operation.
/// </summary>
/// <typeparam name="T">The type used by the operation.</typeparam>
/// <returns>The result of the T operation.</returns>
{
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    ForgeSchemaDiff GenerateDiff<T>();
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<ForgeSchemaDiff> GenerateDiffAsync<T>(CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    ForgeSchemaVerificationResult VerifySchema<T>();
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<ForgeSchemaVerificationResult> VerifySchemaAsync<T>(CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    string SyncSchema<T>();
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<string> SyncSchemaAsync<T>(CancellationToken cancellationToken = default);
}

public interface IForgeSmartQuery<T>
/// <summary>
/// Defines the WhereSql operation.
/// </summary>
/// <param name="sql">The sql value.</param>
/// <returns>The result of the WhereSql operation.</returns>
{
    /// <summary>
    /// Defines the WhereSql operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the WhereSql operation.</returns>
    IForgeSmartQuery<T> WhereSql(FormattableString sql);
    /// <summary>
    /// Defines the WithPolicy operation.
    /// </summary>
    /// <param name="policy">The policy value.</param>
    /// <returns>The result of the WithPolicy operation.</returns>
    IForgeSmartQuery<T> WithPolicy(ForgeResiliencePolicy policy);
    /// <summary>
    /// Defines the AsCached operation.
    /// </summary>
    /// <param name="duration">The duration value.</param>
    /// <param name="key">The key value.</param>
    /// <returns>The result of the AsCached operation.</returns>
    IForgeSmartQuery<T> AsCached(TimeSpan duration, string? key = null);
    /// <summary>
    /// Defines the Mock operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the Mock operation.</returns>
    IForgeSmartQuery<T> Mock(IEnumerable<T> rows);
    /// <summary>
    /// Defines the IncludeGraph operation.
    /// </summary>
    /// <param name="maxDepth">The maxDepth value.</param>
    /// <returns>The result of the IncludeGraph operation.</returns>
    IForgeSmartQuery<T> IncludeGraph(int maxDepth = 2);
    /// <summary>
    /// Defines the ShadowProperty operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the ShadowProperty operation.</returns>
    IForgeSmartQuery<T> ShadowProperty(string name);

/// <summary>

/// Defines the ExecuteTransparent operation.

/// </summary>

/// <returns>The result of the ExecuteTransparent operation.</returns>

    /// <summary>
    /// Defines the ExecuteTransparent operation.
    /// </summary>
    /// <returns>The result of the ExecuteTransparent operation.</returns>
    ForgeTransparentCommand ExecuteTransparent();
    /// <summary>
    /// Defines the Explain operation.
    /// </summary>
    /// <returns>The result of the Explain operation.</returns>
    ForgeExplainResult Explain();
    /// <summary>
    /// Defines the ExplainAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ExplainAsync operation.</returns>
    Task<ForgeExplainResult> ExplainAsync(CancellationToken cancellationToken = default);

/// <summary>

/// Defines the TShape operation.

/// </summary>

/// <typeparam name="TShape">The type used by the operation.</typeparam>

/// <returns>The result of the TShape operation.</returns>

    /// <summary>
    /// Defines the TShape operation.
    /// </summary>
    /// <typeparam name="TShape">The type used by the operation.</typeparam>
    /// <returns>The result of the TShape operation.</returns>
    IReadOnlyList<TShape> ToShape<TShape>();
    /// <summary>
    /// Defines the TShape operation.
    /// </summary>
    /// <typeparam name="TShape">The type used by the operation.</typeparam>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the TShape operation.</returns>
    Task<IReadOnlyList<TShape>> ToShapeAsync<TShape>(CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the TShape operation.
    /// </summary>
    /// <typeparam name="TShape">The type used by the operation.</typeparam>
    /// <returns>The result of the TShape operation.</returns>
    IReadOnlyList<TShape> MapStatic<TShape>();
    /// <summary>
    /// Defines the TShape operation.
    /// </summary>
    /// <typeparam name="TShape">The type used by the operation.</typeparam>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the TShape operation.</returns>
    Task<IReadOnlyList<TShape>> MapStaticAsync<TShape>(CancellationToken cancellationToken = default);

/// <summary>

/// Defines the IntoJsonDocument operation.

/// </summary>

/// <returns>The result of the IntoJsonDocument operation.</returns>

    /// <summary>
    /// Defines the IntoJsonDocument operation.
    /// </summary>
    /// <returns>The result of the IntoJsonDocument operation.</returns>
    JsonDocument IntoJsonDocument();
    /// <summary>
    /// Defines the IntoJsonDocumentAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the IntoJsonDocumentAsync operation.</returns>
    Task<JsonDocument> IntoJsonDocumentAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the IntoJson operation.
    /// </summary>
    /// <returns>The result of the IntoJson operation.</returns>
    string IntoJson();
    /// <summary>
    /// Defines the IntoJsonAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the IntoJsonAsync operation.</returns>
    Task<string> IntoJsonAsync(CancellationToken cancellationToken = default);

/// <summary>

/// Defines the StreamAllAsync operation.

/// </summary>

/// <param name="cancellationToken">The cancellationToken value.</param>

/// <returns>The result of the StreamAllAsync operation.</returns>

    /// <summary>
    /// Defines the StreamAllAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the StreamAllAsync operation.</returns>
    IAsyncEnumerable<T> StreamAllAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the ToList operation.
    /// </summary>
    /// <returns>The result of the ToList operation.</returns>
    IReadOnlyList<T> ToList();
    /// <summary>
    /// Defines the ToListAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ToListAsync operation.</returns>
    Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default);
}

public sealed class ForgeSafeSql
{
    public required string Sql { get; init; }
    public required Dictionary<string, object?> Parameters { get; init; }
}

public static class ForgeSqlSafety
{
    /// <summary>
    /// Executes the From operation.
    /// </summary>
    /// <param name="formattable">The formattable value.</param>
    /// <returns>The result of the From operation.</returns>
    public static ForgeSafeSql From(FormattableString formattable)
    {
        var sql = formattable.Format;
        var args = formattable.GetArguments();
        var parameters = new Dictionary<string, object?>();

        for (var i = 0; i < args.Length; i++)
        {
            var name = $"p{i}";
            sql = sql.Replace("{" + i + "}", "@" + name);
            parameters[name] = args[i];
        }

        return new ForgeSafeSql { Sql = sql, Parameters = parameters };
    }
}
