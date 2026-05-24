using ForgeORM.Abstractions;
using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.VectorSearch;

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
