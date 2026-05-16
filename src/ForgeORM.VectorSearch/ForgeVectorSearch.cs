using Microsoft.Extensions.DependencyInjection;
using ForgeORM.Abstractions;
using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.VectorSearch;

public sealed record ForgeVectorDocument(
    string Id,
    float[] Vector,
    string Text,
    IReadOnlyDictionary<string, string> Metadata);
public sealed record ForgeVectorSearchResult(string Id, string Text, double Score, IReadOnlyDictionary<string, string>? Metadata = null);

public interface IForgeVectorStore
/// <summary>
/// Defines the UpsertAsync operation.
/// </summary>
/// <param name="document">The document value.</param>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the UpsertAsync operation.</returns>
{
    /// <summary>
    /// Defines the UpsertAsync operation.
    /// </summary>
    /// <param name="document">The document value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the UpsertAsync operation.</returns>
    Task UpsertAsync(ForgeVectorDocument document, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the SearchAsync operation.
    /// </summary>
    /// <param name="floatqueryVector">The floatqueryVector value.</param>
    /// <param name="topK">The topK value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the SearchAsync operation.</returns>
    Task<IReadOnlyList<ForgeVectorSearchResult>> SearchAsync(float[] queryVector, int topK = 5, CancellationToken cancellationToken = default);
}

public sealed class ForgeInMemoryVectorStore : IForgeVectorStore
{
    private readonly List<ForgeVectorDocument> _documents = [];
    private readonly object _gate = new();

    /// <summary>
    /// Executes the UpsertAsync operation.
    /// </summary>
    /// <param name="document">The document value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the UpsertAsync operation.</returns>
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
    /// Executes the SearchAsync operation.
    /// </summary>
    /// <param name="floatqueryVector">The floatqueryVector value.</param>
    /// <param name="topK">The topK value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the SearchAsync operation.</returns>
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
    /// Executes the CosineSimilarity operation.
    /// </summary>
    /// <param name="a">The a value.</param>
    /// <param name="b">The b value.</param>
    /// <returns>The result of the CosineSimilarity operation.</returns>
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
    /// Executes the BuildSqlServerVectorSearch operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="idColumn">The idColumn value.</param>
    /// <param name="textColumn">The textColumn value.</param>
    /// <param name="vectorColumn">The vectorColumn value.</param>
    /// <param name="topK">The topK value.</param>
    /// <returns>The result of the BuildSqlServerVectorSearch operation.</returns>
    public string BuildSqlServerVectorSearch(string table, string idColumn, string textColumn, string vectorColumn, int topK)
        => $"SELECT TOP ({topK}) {idColumn}, {textColumn}, VECTOR_DISTANCE('cosine', {vectorColumn}, @Vector) AS Score FROM {table} ORDER BY Score";

    /// <summary>
    /// Executes the BuildPostgreSqlPgVectorSearch operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="idColumn">The idColumn value.</param>
    /// <param name="textColumn">The textColumn value.</param>
    /// <param name="vectorColumn">The vectorColumn value.</param>
    /// <param name="topK">The topK value.</param>
    /// <returns>The result of the BuildPostgreSqlPgVectorSearch operation.</returns>
    public string BuildPostgreSqlPgVectorSearch(string table, string idColumn, string textColumn, string vectorColumn, int topK)
        => $"SELECT {idColumn}, {textColumn}, ({vectorColumn} <=> @Vector) AS score FROM {table} ORDER BY {vectorColumn} <=> @Vector LIMIT {topK}";

    /// <summary>
    /// Builds SQL Server vector-search SQL using table and column metadata from expressions.
    /// </summary>
    /// <typeparam name="TDocument">The vector document entity type.</typeparam>
    /// <param name="idColumn">The document id column expression.</param>
    /// <param name="textColumn">The searchable text column expression.</param>
    /// <param name="vectorColumn">The vector column expression.</param>
    /// <param name="topK">The maximum number of rows to return.</param>
    /// <returns>The rendered SQL Server vector-search query.</returns>
    public string BuildSqlServerVectorSearch<TDocument>(
        Expression<Func<TDocument, object>> idColumn,
        Expression<Func<TDocument, object>> textColumn,
        Expression<Func<TDocument, object>> vectorColumn,
        int topK = 5)
    {
        return BuildSqlServerVectorSearch(
            ResolveTableName(typeof(TDocument)),
            MemberName(idColumn),
            MemberName(textColumn),
            MemberName(vectorColumn),
            topK);
    }

    /// <summary>
    /// Builds PostgreSQL pgvector-search SQL using table and column metadata from expressions.
    /// </summary>
    /// <typeparam name="TDocument">The vector document entity type.</typeparam>
    /// <param name="idColumn">The document id column expression.</param>
    /// <param name="textColumn">The searchable text column expression.</param>
    /// <param name="vectorColumn">The vector column expression.</param>
    /// <param name="topK">The maximum number of rows to return.</param>
    /// <returns>The rendered PostgreSQL pgvector-search query.</returns>
    public string BuildPostgreSqlPgVectorSearch<TDocument>(
        Expression<Func<TDocument, object>> idColumn,
        Expression<Func<TDocument, object>> textColumn,
        Expression<Func<TDocument, object>> vectorColumn,
        int topK = 5)
    {
        return BuildPostgreSqlPgVectorSearch(
            ResolveTableName(typeof(TDocument)),
            MemberName(idColumn),
            MemberName(textColumn),
            MemberName(vectorColumn),
            topK);
    }

    private static string ResolveTableName(Type type)
        => type.GetCustomAttribute<ForgeTableAttribute>()?.Name ?? type.Name;

    private static string MemberName<T>(Expression<Func<T, object>> expression)
    {
        Expression body = expression.Body is UnaryExpression unary ? unary.Operand : expression.Body;
        if (body is not MemberExpression member)
            throw new NotSupportedException("Only member expressions are supported.");
        return member.Member.GetCustomAttribute<ForgeColumnAttribute>()?.Name ?? member.Member.Name;
    }
}

public static class ForgeVectorServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeInMemoryVectorSearch operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeInMemoryVectorSearch operation.</returns>
    public static IServiceCollection AddForgeInMemoryVectorSearch(this IServiceCollection services)
    {
        services.AddSingleton<IForgeVectorStore, ForgeInMemoryVectorStore>();
        services.AddSingleton<ForgeVectorSqlBuilder>();
        return services;
    }
}
