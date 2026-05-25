using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    /// <summary>
    /// Gets multiple entities by their configured key column.
    /// </summary>
    /// <typeparam name="T">The entity type to read.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="ids">The key values to match.</param>
    /// <returns>The entities whose key values match the supplied <paramref name="ids"/>.</returns>
    public IReadOnlyList<T> GetByIds<T, TKey>(IEnumerable<TKey> ids)
    {
        var idList = NormalizeIds(ids);
        if (idList.Count == 0)
            return [];

        var metadata = _metadata.Resolve<T>();
        var sql = $"SELECT * FROM {metadata.TableName} WHERE {metadata.KeyColumn} IN @Ids";
        return Query<T>(sql, new Dictionary<string, object?> { ["Ids"] = idList }).ToList();
    }

    /// <summary>
    /// Gets multiple entities by a key column selected with an expression.
    /// </summary>
    /// <typeparam name="T">The entity type to read.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keySelector">The expression that selects the key column.</param>
    /// <param name="ids">The key values to match.</param>
    /// <returns>The entities whose selected key values match the supplied <paramref name="ids"/>.</returns>
    public IReadOnlyList<T> GetByIds<T, TKey>(Expression<Func<T, TKey>> keySelector, IEnumerable<TKey> ids)
    {
        var idList = NormalizeIds(ids);
        if (idList.Count == 0)
            return [];

        var metadata = _metadata.Resolve<T>();
        var keyColumn = ResolveColumnName(metadata, GetMemberName(keySelector));
        var sql = $"SELECT * FROM {metadata.TableName} WHERE {keyColumn} IN @Ids";
        return Query<T>(sql, new Dictionary<string, object?> { ["Ids"] = idList }).ToList();
    }

    /// <summary>
    /// Gets multiple entities by an explicit SQL key column.
    /// </summary>
    /// <typeparam name="T">The entity type to read.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keyColumn">The SQL column name used in the IN condition.</param>
    /// <param name="ids">The key values to match.</param>
    /// <returns>The entities whose SQL key column values match the supplied <paramref name="ids"/>.</returns>
    public IReadOnlyList<T> GetByIdsSql<T, TKey>(string keyColumn, IEnumerable<TKey> ids)
    {
        var idList = NormalizeIds(ids);
        if (idList.Count == 0)
            return [];

        var metadata = _metadata.Resolve<T>();
        var sql = $"SELECT * FROM {metadata.TableName} WHERE {keyColumn} IN @Ids";
        return Query<T>(sql, new Dictionary<string, object?> { ["Ids"] = idList }).ToList();
    }

    /// <summary>
    /// Gets multiple entities asynchronously by their configured key column.
    /// </summary>
    /// <typeparam name="T">The entity type to read.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="ids">The key values to match.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The entities whose key values match the supplied <paramref name="ids"/>.</returns>
    public ValueTask<IReadOnlyList<T>> GetByIdsAsync<T, TKey>(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
    {
        var idList = NormalizeIds(ids);
        if (idList.Count == 0)
            return ValueTask.FromResult<IReadOnlyList<T>>([]);

        var metadata = _metadata.Resolve<T>();
        var sql = $"SELECT * FROM {metadata.TableName} WHERE {metadata.KeyColumn} IN @Ids";
        return QueryAsync<T>(sql, new Dictionary<string, object?> { ["Ids"] = idList }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets multiple entities asynchronously by a key column selected with an expression.
    /// </summary>
    /// <typeparam name="T">The entity type to read.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keySelector">The expression that selects the key column.</param>
    /// <param name="ids">The key values to match.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The entities whose selected key values match the supplied <paramref name="ids"/>.</returns>
    public ValueTask<IReadOnlyList<T>> GetByIdsAsync<T, TKey>(Expression<Func<T, TKey>> keySelector, IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
    {
        var idList = NormalizeIds(ids);
        if (idList.Count == 0)
            return ValueTask.FromResult<IReadOnlyList<T>>([]);

        var metadata = _metadata.Resolve<T>();
        var keyColumn = ResolveColumnName(metadata, GetMemberName(keySelector));
        var sql = $"SELECT * FROM {metadata.TableName} WHERE {keyColumn} IN @Ids";
        return QueryAsync<T>(sql, new Dictionary<string, object?> { ["Ids"] = idList }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets multiple entities asynchronously by an explicit SQL key column.
    /// </summary>
    /// <typeparam name="T">The entity type to read.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keyColumn">The SQL column name used in the IN condition.</param>
    /// <param name="ids">The key values to match.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The entities whose SQL key column values match the supplied <paramref name="ids"/>.</returns>
    public ValueTask<IReadOnlyList<T>> GetByIdsSqlAsync<T, TKey>(string keyColumn, IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
    {
        var idList = NormalizeIds(ids);
        if (idList.Count == 0)
            return ValueTask.FromResult<IReadOnlyList<T>>([]);

        var metadata = _metadata.Resolve<T>();
        var sql = $"SELECT * FROM {metadata.TableName} WHERE {keyColumn} IN @Ids";
        return QueryAsync<T>(sql, new Dictionary<string, object?> { ["Ids"] = idList }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Deletes multiple entities by their configured key column.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="ids">The key values to delete.</param>
    /// <returns>The number of deleted rows.</returns>
    public int DeleteByIds<T, TKey>(IEnumerable<TKey> ids)
    {
        var idList = NormalizeIds(ids);
        if (idList.Count == 0)
            return 0;

        var metadata = _metadata.Resolve<T>();
        return Execute($"DELETE FROM {metadata.TableName} WHERE {metadata.KeyColumn} IN @Ids", new Dictionary<string, object?> { ["Ids"] = idList });
    }

    /// <summary>
    /// Deletes multiple entities by a key column selected with an expression.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keySelector">The expression that selects the key column.</param>
    /// <param name="ids">The key values to delete.</param>
    /// <returns>The number of deleted rows.</returns>
    public int DeleteByIds<T, TKey>(Expression<Func<T, TKey>> keySelector, IEnumerable<TKey> ids)
    {
        var idList = NormalizeIds(ids);
        if (idList.Count == 0)
            return 0;

        var metadata = _metadata.Resolve<T>();
        var keyColumn = ResolveColumnName(metadata, GetMemberName(keySelector));
        return Execute($"DELETE FROM {metadata.TableName} WHERE {keyColumn} IN @Ids", new Dictionary<string, object?> { ["Ids"] = idList });
    }

    /// <summary>
    /// Deletes multiple entities by an explicit SQL key column.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keyColumn">The SQL column name used in the IN condition.</param>
    /// <param name="ids">The key values to delete.</param>
    /// <returns>The number of deleted rows.</returns>
    public int DeleteByIdsSql<T, TKey>(string keyColumn, IEnumerable<TKey> ids)
    {
        var idList = NormalizeIds(ids);
        if (idList.Count == 0)
            return 0;

        var metadata = _metadata.Resolve<T>();
        return Execute($"DELETE FROM {metadata.TableName} WHERE {keyColumn} IN @Ids", new Dictionary<string, object?> { ["Ids"] = idList });
    }

    /// <summary>
    /// Deletes multiple entities asynchronously by their configured key column.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="ids">The key values to delete.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of deleted rows.</returns>
    public ValueTask<int> DeleteByIdsAsync<T, TKey>(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
    {
        var idList = NormalizeIds(ids);
        if (idList.Count == 0)
            return ValueTask.FromResult(0);

        var metadata = _metadata.Resolve<T>();
        return ExecuteAsync($"DELETE FROM {metadata.TableName} WHERE {metadata.KeyColumn} IN @Ids", new Dictionary<string, object?> { ["Ids"] = idList }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Deletes multiple entities asynchronously by a key column selected with an expression.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keySelector">The expression that selects the key column.</param>
    /// <param name="ids">The key values to delete.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of deleted rows.</returns>
    public ValueTask<int> DeleteByIdsAsync<T, TKey>(Expression<Func<T, TKey>> keySelector, IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
    {
        var idList = NormalizeIds(ids);
        if (idList.Count == 0)
            return ValueTask.FromResult(0);

        var metadata = _metadata.Resolve<T>();
        var keyColumn = ResolveColumnName(metadata, GetMemberName(keySelector));
        return ExecuteAsync($"DELETE FROM {metadata.TableName} WHERE {keyColumn} IN @Ids", new Dictionary<string, object?> { ["Ids"] = idList }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Deletes multiple entities asynchronously by an explicit SQL key column.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keyColumn">The SQL column name used in the IN condition.</param>
    /// <param name="ids">The key values to delete.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of deleted rows.</returns>
    public ValueTask<int> DeleteByIdsSqlAsync<T, TKey>(string keyColumn, IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
    {
        var idList = NormalizeIds(ids);
        if (idList.Count == 0)
            return ValueTask.FromResult(0);

        var metadata = _metadata.Resolve<T>();
        return ExecuteAsync($"DELETE FROM {metadata.TableName} WHERE {keyColumn} IN @Ids", new Dictionary<string, object?> { ["Ids"] = idList }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Updates multiple entities by their configured key column.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="ids">The key values to update.</param>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <returns>The number of updated rows.</returns>
    public int UpdateByIds<T, TKey>(IEnumerable<TKey> ids, object values)
    {
        var idList = NormalizeIds(ids);
        if (idList.Count == 0)
            return 0;

        var metadata = _metadata.Resolve<T>();
        var update = BuildUpdateSet(metadata, values);
        update.Parameters["Ids"] = idList;
        return Execute($"UPDATE {metadata.TableName} SET {update.SetClause} WHERE {metadata.KeyColumn} IN @Ids", update.Parameters);
    }

    /// <summary>
    /// Updates multiple entities by a key column selected with an expression.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keySelector">The expression that selects the key column.</param>
    /// <param name="ids">The key values to update.</param>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <returns>The number of updated rows.</returns>
    public int UpdateByIds<T, TKey>(Expression<Func<T, TKey>> keySelector, IEnumerable<TKey> ids, object values)
    {
        var idList = NormalizeIds(ids);
        if (idList.Count == 0)
            return 0;

        var metadata = _metadata.Resolve<T>();
        var keyColumn = ResolveColumnName(metadata, GetMemberName(keySelector));
        var update = BuildUpdateSet(metadata, values);
        update.Parameters["Ids"] = idList;
        return Execute($"UPDATE {metadata.TableName} SET {update.SetClause} WHERE {keyColumn} IN @Ids", update.Parameters);
    }

    /// <summary>
    /// Updates multiple entities by an explicit SQL key column.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keyColumn">The SQL column name used in the IN condition.</param>
    /// <param name="ids">The key values to update.</param>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <returns>The number of updated rows.</returns>
    public int UpdateByIdsSql<T, TKey>(string keyColumn, IEnumerable<TKey> ids, object values)
    {
        var idList = NormalizeIds(ids);
        if (idList.Count == 0)
            return 0;

        var metadata = _metadata.Resolve<T>();
        var update = BuildUpdateSet(metadata, values);
        update.Parameters["Ids"] = idList;
        return Execute($"UPDATE {metadata.TableName} SET {update.SetClause} WHERE {keyColumn} IN @Ids", update.Parameters);
    }

    /// <summary>
    /// Updates multiple entities asynchronously by their configured key column.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="ids">The key values to update.</param>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of updated rows.</returns>
    public ValueTask<int> UpdateByIdsAsync<T, TKey>(IEnumerable<TKey> ids, object values, CancellationToken cancellationToken = default)
    {
        var idList = NormalizeIds(ids);
        if (idList.Count == 0)
            return ValueTask.FromResult(0);

        var metadata = _metadata.Resolve<T>();
        var update = BuildUpdateSet(metadata, values);
        update.Parameters["Ids"] = idList;
        return ExecuteAsync($"UPDATE {metadata.TableName} SET {update.SetClause} WHERE {metadata.KeyColumn} IN @Ids", update.Parameters, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Updates multiple entities asynchronously by a key column selected with an expression.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keySelector">The expression that selects the key column.</param>
    /// <param name="ids">The key values to update.</param>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of updated rows.</returns>
    public ValueTask<int> UpdateByIdsAsync<T, TKey>(Expression<Func<T, TKey>> keySelector, IEnumerable<TKey> ids, object values, CancellationToken cancellationToken = default)
    {
        var idList = NormalizeIds(ids);
        if (idList.Count == 0)
            return ValueTask.FromResult(0);

        var metadata = _metadata.Resolve<T>();
        var keyColumn = ResolveColumnName(metadata, GetMemberName(keySelector));
        var update = BuildUpdateSet(metadata, values);
        update.Parameters["Ids"] = idList;
        return ExecuteAsync($"UPDATE {metadata.TableName} SET {update.SetClause} WHERE {keyColumn} IN @Ids", update.Parameters, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Updates multiple entities asynchronously by an explicit SQL key column.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keyColumn">The SQL column name used in the IN condition.</param>
    /// <param name="ids">The key values to update.</param>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of updated rows.</returns>
    public ValueTask<int> UpdateByIdsSqlAsync<T, TKey>(string keyColumn, IEnumerable<TKey> ids, object values, CancellationToken cancellationToken = default)
    {
        var idList = NormalizeIds(ids);
        if (idList.Count == 0)
            return ValueTask.FromResult(0);

        var metadata = _metadata.Resolve<T>();
        var update = BuildUpdateSet(metadata, values);
        update.Parameters["Ids"] = idList;
        return ExecuteAsync($"UPDATE {metadata.TableName} SET {update.SetClause} WHERE {keyColumn} IN @Ids", update.Parameters, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Deletes entities that match an expression condition.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <param name="predicate">The expression condition used to select rows for deletion.</param>
    /// <returns>The number of deleted rows.</returns>
    public int DeleteByCondition<T>(Expression<Func<T, bool>> predicate)
    {
        var metadata = _metadata.Resolve<T>();
        var condition = ForgeCoreExpressionSql.Translate(predicate, metadata);
        return Execute($"DELETE FROM {metadata.TableName} WHERE {condition.Sql}", condition.Parameters);
    }

    /// <summary>
    /// Deletes entities that match a SQL condition.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <param name="sqlCondition">The SQL condition used after the WHERE keyword.</param>
    /// <param name="parameters">The SQL condition parameters.</param>
    /// <returns>The number of deleted rows.</returns>
    public int DeleteByConditionSql<T>(string sqlCondition, object? parameters = null)
    {
        var metadata = _metadata.Resolve<T>();
        return Execute($"DELETE FROM {metadata.TableName} WHERE {sqlCondition}", parameters);
    }

    /// <summary>
    /// Deletes entities asynchronously that match an expression condition.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <param name="predicate">The expression condition used to select rows for deletion.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of deleted rows.</returns>
    public ValueTask<int> DeleteByConditionAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var metadata = _metadata.Resolve<T>();
        var condition = ForgeCoreExpressionSql.Translate(predicate, metadata);
        return ExecuteAsync($"DELETE FROM {metadata.TableName} WHERE {condition.Sql}", condition.Parameters, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Deletes entities asynchronously that match a SQL condition.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <param name="sqlCondition">The SQL condition used after the WHERE keyword.</param>
    /// <param name="parameters">The SQL condition parameters.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of deleted rows.</returns>
    public ValueTask<int> DeleteByConditionSqlAsync<T>(string sqlCondition, object? parameters = null, CancellationToken cancellationToken = default)
    {
        var metadata = _metadata.Resolve<T>();
        return ExecuteAsync($"DELETE FROM {metadata.TableName} WHERE {sqlCondition}", parameters, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Updates entities that match an expression condition.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <param name="predicate">The expression condition used to select rows for update.</param>
    /// <returns>The number of updated rows.</returns>
    public int UpdateByCondition<T>(object values, Expression<Func<T, bool>> predicate)
    {
        var metadata = _metadata.Resolve<T>();
        var update = BuildUpdateSet(metadata, values);
        var condition = ForgeCoreExpressionSql.Translate(predicate, metadata, update.Parameters.Count);
        Merge(update.Parameters, condition.Parameters);
        return Execute($"UPDATE {metadata.TableName} SET {update.SetClause} WHERE {condition.Sql}", update.Parameters);
    }

    /// <summary>
    /// Updates entities that match a SQL condition.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <param name="sqlCondition">The SQL condition used after the WHERE keyword.</param>
    /// <param name="parameters">The SQL condition parameters.</param>
    /// <returns>The number of updated rows.</returns>
    public int UpdateByConditionSql<T>(object values, string sqlCondition, object? parameters = null)
    {
        var metadata = _metadata.Resolve<T>();
        var update = BuildUpdateSet(metadata, values);
        Merge(update.Parameters, ToDictionary(parameters));
        return Execute($"UPDATE {metadata.TableName} SET {update.SetClause} WHERE {sqlCondition}", update.Parameters);
    }

    /// <summary>
    /// Updates entities asynchronously that match an expression condition.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <param name="predicate">The expression condition used to select rows for update.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of updated rows.</returns>
    public ValueTask<int> UpdateByConditionAsync<T>(object values, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var metadata = _metadata.Resolve<T>();
        var update = BuildUpdateSet(metadata, values);
        var condition = ForgeCoreExpressionSql.Translate(predicate, metadata, update.Parameters.Count);
        Merge(update.Parameters, condition.Parameters);
        return ExecuteAsync($"UPDATE {metadata.TableName} SET {update.SetClause} WHERE {condition.Sql}", update.Parameters, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Updates entities asynchronously that match a SQL condition.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <param name="sqlCondition">The SQL condition used after the WHERE keyword.</param>
    /// <param name="parameters">The SQL condition parameters.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of updated rows.</returns>
    public ValueTask<int> UpdateByConditionSqlAsync<T>(object values, string sqlCondition, object? parameters = null, CancellationToken cancellationToken = default)
    {
        var metadata = _metadata.Resolve<T>();
        var update = BuildUpdateSet(metadata, values);
        Merge(update.Parameters, ToDictionary(parameters));
        return ExecuteAsync($"UPDATE {metadata.TableName} SET {update.SetClause} WHERE {sqlCondition}", update.Parameters, cancellationToken: cancellationToken);
    }

    private static IReadOnlyList<object?> NormalizeIds<TKey>(IEnumerable<TKey> ids)
        => ids.Select(x => (object?)x).Where(x => x is not null).Distinct().ToList();

    private static ForgeUpdateSet BuildUpdateSet(ForgeEntityMetadata metadata, object values)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var assignments = new List<string>();

        foreach (var prop in values.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanRead))
        {
            var propertyName = prop.Name;
            var columnName = ResolveColumnName(metadata, propertyName);
            var parameterName = "set_" + propertyName;
            assignments.Add($"{columnName} = @{parameterName}");
            parameters[parameterName] = ForgeRuntimeAccessorCache.Get(prop, values);
        }

        if (assignments.Count == 0)
            throw new InvalidOperationException("At least one update value is required.");

        return new ForgeUpdateSet(string.Join(", ", assignments), parameters);
    }

    private static string ResolveColumnName(ForgeEntityMetadata metadata, string propertyName)
        => metadata.Properties.FirstOrDefault(x => x.PropertyName.Equals(propertyName, StringComparison.OrdinalIgnoreCase))?.ColumnName ?? propertyName;

    private static string GetMemberName(LambdaExpression expression)
    {
        Expression body = expression.Body;
        if (body is UnaryExpression unary)
            body = unary.Operand;

        return body is MemberExpression member
            ? member.Member.Name
            : throw new NotSupportedException("Only simple member selector expressions are supported.");
    }

    private static Dictionary<string, object?> ToDictionary(object? parameters)
    {
        if (parameters is null)
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (parameters is IDictionary<string, object?> dictionary)
            return new Dictionary<string, object?>(dictionary, StringComparer.OrdinalIgnoreCase);

        if (parameters is IReadOnlyDictionary<string, object?> readOnlyDictionary)
            return new Dictionary<string, object?>(readOnlyDictionary, StringComparer.OrdinalIgnoreCase);

        return parameters.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.CanRead)
            .ToDictionary(x => x.Name, x => ForgeRuntimeAccessorCache.Get(x, parameters), StringComparer.OrdinalIgnoreCase);
    }

    private static void Merge(IDictionary<string, object?> target, IReadOnlyDictionary<string, object?> source)
    {
        foreach (var item in source)
            target[item.Key] = item.Value;
    }

    private sealed record ForgeUpdateSet(string SetClause, Dictionary<string, object?> Parameters);
}
