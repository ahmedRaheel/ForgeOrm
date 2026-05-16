using ForgeORM.Abstractions;
using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.QueryAst;

/// <summary>
/// Provides user-friendly shortcuts for QueryAst so callers can use expression-first APIs with minimal parameters.
/// </summary>
public static class ForgeEasyQueryExtensions
{
    /// <summary>
    /// Selects all public writable projection properties by name.
    /// </summary>
    /// <typeparam name="TSource">The source entity type.</typeparam>
    /// <typeparam name="TProjection">The projection type to select into.</typeparam>
    /// <param name="builder">The select builder to configure.</param>
    /// <returns>The same select builder for fluent chaining.</returns>
    public static IForgeAstSelectBuilder<TSource> ProjectTo<TSource, TProjection>(this IForgeAstSelectBuilder<TSource> builder)
    {
        var columns = typeof(TProjection)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanWrite || property.GetSetMethod(nonPublic: true) is not null)
            .Select(property => ResolveColumnName(property))
            .ToArray();

        return builder.ColumnsSql(columns);
    }

    /// <summary>
    /// Adds a selected source column and aliases it to a projection property name.
    /// </summary>
    /// <typeparam name="TSource">The source entity type.</typeparam>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    /// <typeparam name="TValue">The selected value type.</typeparam>
    /// <param name="builder">The select builder to configure.</param>
    /// <param name="sourceColumn">The source column expression.</param>
    /// <param name="projectionProperty">The target projection property expression.</param>
    /// <returns>The same select builder for fluent chaining.</returns>
    public static IForgeAstSelectBuilder<TSource> ColumnAs<TSource, TProjection, TValue>(
        this IForgeAstSelectBuilder<TSource> builder,
        Expression<Func<TSource, TValue>> sourceColumn,
        Expression<Func<TProjection, TValue>> projectionProperty)
    {
        return builder.ColumnsSql($"{MemberName(sourceColumn)} AS {MemberName(projectionProperty)}");
    }

    /// <summary>
    /// Adds selected source columns using expressions instead of string column names.
    /// </summary>
    /// <typeparam name="TSource">The source entity type.</typeparam>
    /// <param name="builder">The select builder to configure.</param>
    /// <param name="columns">The source column expressions to select.</param>
    /// <returns>The same select builder for fluent chaining.</returns>
    public static IForgeAstSelectBuilder<TSource> SelectColumns<TSource>(
        this IForgeAstSelectBuilder<TSource> builder,
        params Expression<Func<TSource, object>>[] columns)
    {
        return builder.Columns(columns);
    }

    /// <summary>
    /// Applies pagination with safe page and page-size defaults.
    /// </summary>
    /// <typeparam name="TSource">The source entity type.</typeparam>
    /// <param name="builder">The select builder to configure.</param>
    /// <param name="page">The requested page number. Values less than one become one.</param>
    /// <param name="pageSize">The requested page size. Values outside the allowed range use the default.</param>
    /// <param name="defaultPageSize">The fallback page size.</param>
    /// <param name="maxPageSize">The maximum allowed page size.</param>
    /// <returns>The same select builder for fluent chaining.</returns>
    public static IForgeAstSelectBuilder<TSource> Page<TSource>(
        this IForgeAstSelectBuilder<TSource> builder,
        int page,
        int pageSize,
        int defaultPageSize = 20,
        int maxPageSize = 200)
    {
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize <= 0 || pageSize > maxPageSize ? defaultPageSize : pageSize;
        return builder.Skip((safePage - 1) * safePageSize).Take(safePageSize);
    }

    /// <summary>
    /// Adds an expression filter only when the supplied value is not null.
    /// </summary>
    /// <typeparam name="TSource">The source entity type.</typeparam>
    /// <typeparam name="TValue">The optional value type.</typeparam>
    /// <param name="builder">The select builder to configure.</param>
    /// <param name="value">The optional value that controls whether the filter is applied.</param>
    /// <param name="predicate">The expression filter to apply when the value exists.</param>
    /// <returns>The same select builder for fluent chaining.</returns>
    public static IForgeAstSelectBuilder<TSource> WhereWhen<TSource, TValue>(
        this IForgeAstSelectBuilder<TSource> builder,
        TValue? value,
        Expression<Func<TSource, bool>> predicate)
    {
        return value is null ? builder : builder.Where(predicate);
    }

    /// <summary>
    /// Adds an expression filter only when the supplied text has content.
    /// </summary>
    /// <typeparam name="TSource">The source entity type.</typeparam>
    /// <param name="builder">The select builder to configure.</param>
    /// <param name="text">The optional text that controls whether the filter is applied.</param>
    /// <param name="predicate">The expression filter to apply when the text has content.</param>
    /// <returns>The same select builder for fluent chaining.</returns>
    public static IForgeAstSelectBuilder<TSource> WhereWhenNotEmpty<TSource>(
        this IForgeAstSelectBuilder<TSource> builder,
        string? text,
        Expression<Func<TSource, bool>> predicate)
    {
        return string.IsNullOrWhiteSpace(text) ? builder : builder.Where(predicate);
    }

    /// <summary>
    /// Renders and executes the select builder as a list query.
    /// </summary>
    /// <typeparam name="TSource">The source entity type used by the builder.</typeparam>
    /// <typeparam name="TResult">The result type returned by the query.</typeparam>
    /// <param name="builder">The select builder to execute.</param>
    /// <param name="db">The ForgeORM database session.</param>
    /// <param name="cancellationToken">The cancellation token for the async operation.</param>
    /// <returns>The result rows.</returns>
    public static Task<IReadOnlyList<TResult>> ToListAsync<TSource, TResult>(
        this IForgeAstSelectBuilder<TSource> builder,
        IForgeDb db,
        CancellationToken cancellationToken = default)
    {
        var rendered = builder.Render(db.Provider);
        return db.QueryAsync<TResult>(rendered.Sql, rendered.Parameters, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Renders and executes the select builder as a first-or-default query.
    /// </summary>
    /// <typeparam name="TSource">The source entity type used by the builder.</typeparam>
    /// <typeparam name="TResult">The result type returned by the query.</typeparam>
    /// <param name="builder">The select builder to execute.</param>
    /// <param name="db">The ForgeORM database session.</param>
    /// <param name="cancellationToken">The cancellation token for the async operation.</param>
    /// <returns>The first result row, or null when no row exists.</returns>
    public static Task<TResult?> FirstOrDefaultAsync<TSource, TResult>(
        this IForgeAstSelectBuilder<TSource> builder,
        IForgeDb db,
        CancellationToken cancellationToken = default)
    {
        var rendered = builder.Take(1).Render(db.Provider);
        return db.QueryFirstOrDefaultAsync<TResult>(rendered.Sql, rendered.Parameters, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Renders and executes the select builder as an existence query.
    /// </summary>
    /// <typeparam name="TSource">The source entity type used by the builder.</typeparam>
    /// <param name="builder">The select builder to execute.</param>
    /// <param name="db">The ForgeORM database session.</param>
    /// <param name="cancellationToken">The cancellation token for the async operation.</param>
    /// <returns>True when at least one row exists; otherwise false.</returns>
    public static async Task<bool> AnyAsync<TSource>(
        this IForgeAstSelectBuilder<TSource> builder,
        IForgeDb db,
        CancellationToken cancellationToken = default)
    {
        var rendered = builder.RenderAny(db.Provider);
        var result = await db.ExecuteScalarAsync<int>(rendered.Sql, rendered.Parameters, cancellationToken: cancellationToken);
        return result > 0;
    }

    /// <summary>
    /// Renders and executes the select builder as a count query.
    /// </summary>
    /// <typeparam name="TSource">The source entity type used by the builder.</typeparam>
    /// <param name="builder">The select builder to execute.</param>
    /// <param name="db">The ForgeORM database session.</param>
    /// <param name="cancellationToken">The cancellation token for the async operation.</param>
    /// <returns>The number of matching rows.</returns>
    public static async Task<int> CountAsync<TSource>(
        this IForgeAstSelectBuilder<TSource> builder,
        IForgeDb db,
        CancellationToken cancellationToken = default)
    {
        var rendered = builder.RenderCount(db.Provider);
        return await db.ExecuteScalarAsync<int>(rendered.Sql, rendered.Parameters, cancellationToken: cancellationToken);
    }

    private static string ResolveColumnName(PropertyInfo property)
    {
        var attr = property.GetCustomAttributes(typeof(ForgeColumnAttribute), false).Cast<ForgeColumnAttribute>().FirstOrDefault();
        return attr?.Name ?? property.Name;
    }

    private static string MemberName<T, TValue>(Expression<Func<T, TValue>> expression)
    {
        Expression body = expression.Body is UnaryExpression unary ? unary.Operand : expression.Body;
        return body is MemberExpression member
            ? member.Member.Name
            : throw new NotSupportedException("Only member expressions are supported.");
    }
}
