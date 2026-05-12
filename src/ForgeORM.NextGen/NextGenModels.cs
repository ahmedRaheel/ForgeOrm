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

    public int Execute(IForgeDb db) => db.Execute(Sql, Parameters);
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
    public int RetryCount { get; init; } = 0;
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMilliseconds(100);
    public bool UseCircuitBreaker { get; init; }
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

    public ForgeMockDataSet Add<T>(IEnumerable<T> rows)
    {
        _tables[typeof(T)] = rows.ToList();
        return this;
    }

    public IReadOnlyList<T> Get<T>()
    {
        if (_tables.TryGetValue(typeof(T), out var rows))
            return rows.Cast<T>().ToList();

        return [];
    }
}

public sealed class ForgeShadowRow<T>
{
    public required T Entity { get; init; }
    public Dictionary<string, object?> ShadowValues { get; init; } = [];
    public object? ShadowProperty(string name) => ShadowValues.TryGetValue(name, out var value) ? value : null;
}

public interface IForgeSchemaManager
{
    ForgeSchemaDiff GenerateDiff<T>();
    Task<ForgeSchemaDiff> GenerateDiffAsync<T>(CancellationToken cancellationToken = default);
    ForgeSchemaVerificationResult VerifySchema<T>();
    Task<ForgeSchemaVerificationResult> VerifySchemaAsync<T>(CancellationToken cancellationToken = default);
    string SyncSchema<T>();
    Task<string> SyncSchemaAsync<T>(CancellationToken cancellationToken = default);
}

public interface IForgeSmartQuery<T>
{
    IForgeSmartQuery<T> WhereSql(FormattableString sql);
    IForgeSmartQuery<T> WithPolicy(ForgeResiliencePolicy policy);
    IForgeSmartQuery<T> AsCached(TimeSpan duration, string? key = null);
    IForgeSmartQuery<T> Mock(IEnumerable<T> rows);
    IForgeSmartQuery<T> IncludeGraph(int maxDepth = 2);
    IForgeSmartQuery<T> ShadowProperty(string name);

    ForgeTransparentCommand ExecuteTransparent();
    ForgeExplainResult Explain();
    Task<ForgeExplainResult> ExplainAsync(CancellationToken cancellationToken = default);

    IReadOnlyList<TShape> ToShape<TShape>();
    Task<IReadOnlyList<TShape>> ToShapeAsync<TShape>(CancellationToken cancellationToken = default);
    IReadOnlyList<TShape> MapStatic<TShape>();
    Task<IReadOnlyList<TShape>> MapStaticAsync<TShape>(CancellationToken cancellationToken = default);

    JsonDocument IntoJsonDocument();
    Task<JsonDocument> IntoJsonDocumentAsync(CancellationToken cancellationToken = default);
    string IntoJson();
    Task<string> IntoJsonAsync(CancellationToken cancellationToken = default);

    IAsyncEnumerable<T> StreamAllAsync(CancellationToken cancellationToken = default);
    IReadOnlyList<T> ToList();
    Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default);
}

public sealed class ForgeSafeSql
{
    public required string Sql { get; init; }
    public required Dictionary<string, object?> Parameters { get; init; }
}

public static class ForgeSqlSafety
{
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
