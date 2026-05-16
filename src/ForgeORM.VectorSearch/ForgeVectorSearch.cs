using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.VectorSearch;

public sealed record ForgeVectorDocument(
    string Id,
    float[] Vector,
    string Text,
    IReadOnlyDictionary<string, string> Metadata);
public sealed record ForgeVectorSearchResult(string Id, string Text, double Score, IReadOnlyDictionary<string, string>? Metadata = null);

public interface IForgeVectorStore
{
    Task UpsertAsync(ForgeVectorDocument document, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ForgeVectorSearchResult>> SearchAsync(float[] queryVector, int topK = 5, CancellationToken cancellationToken = default);
}

public sealed class ForgeInMemoryVectorStore : IForgeVectorStore
{
    private readonly List<ForgeVectorDocument> _documents = [];
    private readonly object _gate = new();

    /// <summary>
    /// Initializes or executes the UpsertAsync operation.
    /// </summary>
    /// <param name="document">The document value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public Task UpsertAsync(ForgeVectorDocument document, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _documents.RemoveAll(x => x.Id == document.Id);
            _documents.Add(document);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Initializes or executes the SearchAsync operation.
    /// </summary>
    /// <param name="queryVector">The queryVector value.</param>
    /// <param name="topK">The topK value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public Task<IReadOnlyList<ForgeVectorSearchResult>> SearchAsync(
     float[] queryVector,
     int topK = 5,
     CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryVector);

        lock (_gate)
        {
            var result = _documents
                .Select(d => new ForgeVectorSearchResult(
                    d.Id,
                    d.Text,
                    ForgeVectorMath.CosineSimilarity(queryVector, d.Vector),
                    d.Metadata))
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .ToList();

            return Task.FromResult<IReadOnlyList<ForgeVectorSearchResult>>(result);
        }
    }
}

public static class ForgeVectorMath
{
    /// <summary>
    /// Initializes or executes the CosineSimilarity operation.
    /// </summary>
    /// <param name="a">The a value.</param>
    /// <param name="b">The b value.</param>
    /// <returns>The operation result.</returns>
    public static double CosineSimilarity(IReadOnlyList<float> a, IReadOnlyList<float> b)
    {
        if (a.Count != b.Count) throw new ArgumentException("Vector dimensions must match.");
        double dot = 0, magA = 0, magB = 0;
        for (var i = 0; i < a.Count; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return magA == 0 || magB == 0 ? 0 : dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}

public sealed class ForgeVectorSqlBuilder
{
    /// <summary>
    /// Initializes or executes the BuildSqlServerVectorSearch operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="idColumn">The idColumn value.</param>
    /// <param name="textColumn">The textColumn value.</param>
    /// <param name="vectorColumn">The vectorColumn value.</param>
    /// <param name="topK">The topK value.</param>
    /// <returns>The operation result.</returns>
    public string BuildSqlServerVectorSearch(string table, string idColumn, string textColumn, string vectorColumn, int topK)
        => $"SELECT TOP ({topK}) {idColumn}, {textColumn}, VECTOR_DISTANCE('cosine', {vectorColumn}, @Vector) AS Score FROM {table} ORDER BY Score";

    /// <summary>
    /// Initializes or executes the BuildPostgreSqlPgVectorSearch operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="idColumn">The idColumn value.</param>
    /// <param name="textColumn">The textColumn value.</param>
    /// <param name="vectorColumn">The vectorColumn value.</param>
    /// <param name="topK">The topK value.</param>
    /// <returns>The operation result.</returns>
    public string BuildPostgreSqlPgVectorSearch(string table, string idColumn, string textColumn, string vectorColumn, int topK)
        => $"SELECT {idColumn}, {textColumn}, ({vectorColumn} <=> @Vector) AS score FROM {table} ORDER BY {vectorColumn} <=> @Vector LIMIT {topK}";
}

public static class ForgeVectorServiceCollectionExtensions
{
    /// <summary>
    /// Initializes or executes the AddForgeInMemoryVectorSearch operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The operation result.</returns>
    public static IServiceCollection AddForgeInMemoryVectorSearch(this IServiceCollection services)
    {
        services.AddSingleton<IForgeVectorStore, ForgeInMemoryVectorStore>();
        services.AddSingleton<ForgeVectorSqlBuilder>();
        return services;
    }
}
