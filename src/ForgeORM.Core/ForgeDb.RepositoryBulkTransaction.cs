using System.Data.Common;
using ForgeORM.Abstractions;
using ForgeORM.Core.Performance;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="c">The c value.</param>
    /// <returns>The result of the T operation.</returns>
    public T? GetById<T>(object id)
    {
        var metadata = _metadata.Resolve<T>();
        if (ForgeSqlServerProviderDirectHotPath.CanUse(Provider))
            return ForgeSqlServerProviderDirectHotPath.GetById<T>(_connectionString, metadata, id);

        var c = Provider.BuildGetById(metadata, id);
        return ForgeFrameworkExecutionPolicy.FirstOrDefault<T, ForgeIdParameter<object?>>(Provider, _connectionString, c.CommandText, ForgeIdParameter<object?>.Create(id), timeoutSeconds: null);
    }
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="id">The id value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public async ValueTask<T?> GetByIdAsync<T>(object id, CancellationToken cancellationToken = default)
    {
        var metadata = _metadata.Resolve<T>();
        if (ForgeSqlServerProviderDirectHotPath.CanUse(Provider))
            return await ForgeSqlServerProviderDirectHotPath.GetByIdAsync<T>(_connectionString, metadata, id, cancellationToken).ConfigureAwait(false);

        var c = Provider.BuildGetById(metadata, id);
        return await ForgeFrameworkExecutionPolicy.FirstOrDefaultAsync<T, ForgeIdParameter<object?>>(Provider, _connectionString, c.CommandText, ForgeIdParameter<object?>.Create(id), timeoutSeconds: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Gets one row by the configured key without boxing the key value at the public API.</summary>
    public T? GetById<T, TKey>(TKey id)
    {
        var metadata = _metadata.Resolve<T>();
        if (ForgeSqlServerProviderDirectHotPath.CanUse(Provider))
            return ForgeSqlServerProviderDirectHotPath.GetById<T>(_connectionString, metadata, id!);

        var c = Provider.BuildGetById(metadata, id!);
        return ForgeFrameworkExecutionPolicy.FirstOrDefault<T, ForgeIdParameter<TKey>>(Provider, _connectionString, c.CommandText, ForgeIdParameter<TKey>.Create(id), timeoutSeconds: null);
    }

    /// <summary>Gets one row by the configured key without boxing the key value at the public API.</summary>
    public async ValueTask<T?> GetByIdAsync<T, TKey>(TKey id, CancellationToken cancellationToken = default)
    {
        var metadata = _metadata.Resolve<T>();
        if (ForgeSqlServerProviderDirectHotPath.CanUse(Provider))
            return await ForgeSqlServerProviderDirectHotPath.GetByIdAsync<T>(_connectionString, metadata, id!, cancellationToken).ConfigureAwait(false);

        var c = Provider.BuildGetById(metadata, id!);
        return await ForgeFrameworkExecutionPolicy.FirstOrDefaultAsync<T, ForgeIdParameter<TKey>>(Provider, _connectionString, c.CommandText, ForgeIdParameter<TKey>.Create(id), timeoutSeconds: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="c">The c value.</param>
    /// <returns>The result of the T operation.</returns>
    public T? GetByCode<T>(string code) { var c = Provider.BuildGetByCode(_metadata.Resolve<T>(), code); return QuerySingleOrDefault<T>(c.CommandText, c.Parameters); }
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="code">The code value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask<T?> GetByCodeAsync<T>(string code, CancellationToken cancellationToken = default) { var c = Provider.BuildGetByCode(_metadata.Resolve<T>(), code); return QuerySingleOrDefaultAsync<T>(c.CommandText, c.Parameters, cancellationToken: cancellationToken); }
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    public IReadOnlyList<T> GetByIds<T>(IReadOnlyCollection<int> ids) { if (ids.Count == 0) return []; var c = Provider.BuildGetByIds(_metadata.Resolve<T>(), ids); return Query<T>(c.CommandText, c.Parameters); }
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="ids">The ids value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask<IReadOnlyList<T>> GetByIdsAsync<T>(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default) { if (ids.Count == 0) return ValueTask.FromResult<IReadOnlyList<T>>([]); var c = Provider.BuildGetByIds(_metadata.Resolve<T>(), ids); return QueryAsync<T>(c.CommandText, c.Parameters, cancellationToken: cancellationToken); }
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="c">The c value.</param>
    /// <returns>The result of the T operation.</returns>
    public int Insert<T>(T entity) => InsertCompiled(entity);
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="entity">The entity value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask<int> InsertAsync<T>(T entity, CancellationToken cancellationToken = default) => InsertCompiledAsync(entity, cancellationToken);
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="c">The c value.</param>
    /// <returns>The result of the T operation.</returns>
    public int Update<T>(T entity)
    {
        var metadata = _metadata.Resolve<T>();
        if (ForgeSqlServerProviderDirectHotPath.CanUse(Provider))
            return ForgeSqlServerProviderDirectHotPath.UpdateEntity(_connectionString, metadata, entity!, timeoutSeconds: null);

        var c = Provider.BuildUpdate(metadata, entity!);
        return Execute(c.CommandText, c.Parameters);
    }
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="entity">The entity value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask<int> UpdateAsync<T>(T entity, CancellationToken cancellationToken = default)
    {
        var metadata = _metadata.Resolve<T>();
        if (ForgeSqlServerProviderDirectHotPath.CanUse(Provider))
            return ForgeSqlServerProviderDirectHotPath.UpdateEntityAsync(_connectionString, metadata, entity!, timeoutSeconds: null, cancellationToken);

        var c = Provider.BuildUpdate(metadata, entity!);
        return ExecuteAsync(c.CommandText, c.Parameters, cancellationToken: cancellationToken);
    }
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="c">The c value.</param>
    /// <returns>The result of the T operation.</returns>
    public int Delete<T>(object id)
    {
        var metadata = _metadata.Resolve<T>();
        if (ForgeSqlServerProviderDirectHotPath.CanUse(Provider))
            return ForgeSqlServerProviderDirectHotPath.DeleteEntity(_connectionString, metadata, id, timeoutSeconds: null);

        var c = Provider.BuildDelete(metadata, id);
        return Execute(c.CommandText, c.Parameters);
    }

    /// <summary>Deletes one row by the configured key without boxing the key value at the public API.</summary>
    public int Delete<T, TKey>(TKey id)
    {
        var metadata = _metadata.Resolve<T>();
        if (ForgeSqlServerProviderDirectHotPath.CanUse(Provider))
            return ForgeSqlServerProviderDirectHotPath.DeleteEntity(_connectionString, metadata, id!, timeoutSeconds: null);

        var c = Provider.BuildDelete(metadata, id!);
        return Execute(c.CommandText, c.Parameters);
    }
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="id">The id value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask<int> DeleteAsync<T>(object id, CancellationToken cancellationToken = default)
    {
        var metadata = _metadata.Resolve<T>();
        if (ForgeSqlServerProviderDirectHotPath.CanUse(Provider))
            return ForgeSqlServerProviderDirectHotPath.DeleteEntityAsync(_connectionString, metadata, id, timeoutSeconds: null, cancellationToken);

        var c = Provider.BuildDelete(metadata, id);
        return ExecuteAsync(c.CommandText, c.Parameters, cancellationToken: cancellationToken);
    }

    /// <summary>Deletes one row by the configured key without boxing the key value at the public API.</summary>
    public ValueTask<int> DeleteAsync<T, TKey>(TKey id, CancellationToken cancellationToken = default)
    {
        var metadata = _metadata.Resolve<T>();
        if (ForgeSqlServerProviderDirectHotPath.CanUse(Provider))
            return ForgeSqlServerProviderDirectHotPath.DeleteEntityAsync(_connectionString, metadata, id!, timeoutSeconds: null, cancellationToken);

        var c = Provider.BuildDelete(metadata, id!);
        return ExecuteAsync(c.CommandText, c.Parameters, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="request">The request value.</param>
    /// <returns>The result of the T operation.</returns>
    public ForgePagedResult<T> Page<T>(ForgePageRequest request)
    {
        var count = Provider.BuildCount(request.Sql, request.Parameters);
        var total = ExecuteScalar<int>(count.CommandText, count.Parameters);
        var page = Provider.BuildPage(request);
        var rows = Query<T>(page.CommandText, page.Parameters);
        return new ForgePagedResult<T> { Items = rows, Page = request.Page, PageSize = request.PageSize, TotalRecords = total };
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="request">The request value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public async ValueTask<ForgePagedResult<T>> PageAsync<T>(ForgePageRequest request, CancellationToken cancellationToken = default)
    {
        var count = Provider.BuildCount(request.Sql, request.Parameters);
        var total = await ExecuteScalarAsync<int>(count.CommandText, count.Parameters, cancellationToken: cancellationToken);
        var page = Provider.BuildPage(request);
        var rows = await QueryAsync<T>(page.CommandText, page.Parameters, cancellationToken: cancellationToken);
        return new ForgePagedResult<T> { Items = rows, Page = request.Page, PageSize = request.PageSize, TotalRecords = total };
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    public IForgeQuery<T> Set<T>() => new ForgeQuery<T>(this, _metadata.Resolve<T>());
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    public IForgeQuery<T> From<T>() => Set<T>();
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the T operation.</returns>
    public IForgeQuery<T> Sql<T>(string sql, object? parameters = null) => new ForgeQuery<T>(this, _metadata.Resolve<T>(), sql, parameters);

    /// <summary>
    /// Executes the TParent operation.
    /// </summary>
    /// <typeparam name="TParent">The type used by the operation.</typeparam>
    /// <returns>The result of the TParent operation.</returns>
    public IForgeSplitQuery<TParent> Split<TParent>() => new ForgeSplitQuery<TParent>(this);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the T operation.</returns>
    public void BulkInsert<T>(IReadOnlyCollection<T> rows) => BulkInsert(_metadata.Resolve<T>().TableName, rows);
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the T operation.</returns>
    public void BulkInsert<T>(string tableName, IReadOnlyCollection<T> rows) => BulkInsertAsync(tableName, rows).GetAwaiter().GetResult();
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="rows">The rows value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask BulkInsertAsync<T>(IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default) => BulkInsertAsync(_metadata.Resolve<T>().TableName, rows, cancellationToken);
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public async ValueTask BulkInsertAsync<T>(string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0) return;
        await InsertBulkAsync(tableName, rows, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Inserts many rows using the provider-native bulk path. SQL Server uses TVP + INSERT SELECT.</summary>
    public ValueTask<int> InsertBulkAsync<T>(IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default)
        => InsertBulkAsync(_metadata.Resolve<T>().TableName, rows, cancellationToken);

    /// <summary>Inserts many rows using the provider-native bulk path. SQL Server uses TVP + INSERT SELECT.</summary>
    public async ValueTask<int> InsertBulkAsync<T>(string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0) return 0;
        var metadata = _metadata.Resolve<T>();
        if (!string.Equals(metadata.TableName, tableName, StringComparison.OrdinalIgnoreCase))
        {
            metadata = new ForgeEntityMetadata
            {
                EntityType = metadata.EntityType,
                TableName = tableName,
                KeyColumn = metadata.KeyColumn,
                CodeColumn = metadata.CodeColumn,
                Properties = metadata.Properties
            };
        }

        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await ForgeSqlServerTvpBulkExecutor.InsertAsync(c, metadata, rows, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <returns>The result of the T operation.</returns>
    public void BulkUpdate<T>(IReadOnlyCollection<T> rows, string keyColumn = "Id") => BulkUpdate(_metadata.Resolve<T>().TableName, rows, keyColumn);
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <returns>The result of the T operation.</returns>
    public void BulkUpdate<T>(string tableName, IReadOnlyCollection<T> rows, string keyColumn = "Id") => BulkUpdateAsync(tableName, rows, keyColumn).GetAwaiter().GetResult();
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask BulkUpdateAsync<T>(IReadOnlyCollection<T> rows, string keyColumn = "Id", CancellationToken cancellationToken = default) => BulkUpdateAsync(_metadata.Resolve<T>().TableName, rows, keyColumn, cancellationToken);
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public async ValueTask BulkUpdateAsync<T>(string tableName, IReadOnlyCollection<T> rows, string keyColumn = "Id", CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0) return;
        await UpdateBulkAsync(tableName, rows, keyColumn, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Updates many rows using the provider-native bulk path. SQL Server uses TVP + MERGE.</summary>
    public ValueTask<int> UpdateBulkAsync<T>(IReadOnlyCollection<T> rows, string keyColumn = "Id", CancellationToken cancellationToken = default)
        => UpdateBulkAsync(_metadata.Resolve<T>().TableName, rows, keyColumn, cancellationToken);

    /// <summary>Updates many rows using the provider-native bulk path. SQL Server uses TVP + MERGE.</summary>
    public async ValueTask<int> UpdateBulkAsync<T>(string tableName, IReadOnlyCollection<T> rows, string keyColumn = "Id", CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0) return 0;
        var metadata = _metadata.Resolve<T>();
        if (!string.Equals(metadata.TableName, tableName, StringComparison.OrdinalIgnoreCase) || !string.Equals(metadata.KeyColumn, keyColumn, StringComparison.OrdinalIgnoreCase))
        {
            metadata = new ForgeEntityMetadata
            {
                EntityType = metadata.EntityType,
                TableName = tableName,
                KeyColumn = keyColumn,
                CodeColumn = metadata.CodeColumn,
                Properties = metadata.Properties
            };
        }

        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await ForgeSqlServerTvpBulkExecutor.UpdateAsync(c, metadata, rows, keyColumn, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="ids">The ids value.</param>
    /// <returns>The result of the T operation.</returns>
    public void BulkDelete<T>(IReadOnlyCollection<int> ids) => BulkDelete(_metadata.Resolve<T>().TableName, ids, _metadata.Resolve<T>().KeyColumn);
    /// <summary>
    /// Executes the BulkDelete operation.
    /// </summary>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="ids">The ids value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    public void BulkDelete(string tableName, IReadOnlyCollection<int> ids, string keyColumn = "Id") => BulkDeleteAsync(tableName, ids, keyColumn).GetAwaiter().GetResult();
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="ids">The ids value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask<int> BulkDeleteAsync<T>(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default) { var m = _metadata.Resolve<T>(); return BulkDeleteAsync(m.TableName, ids, m.KeyColumn, cancellationToken); }
    /// <summary>
    /// Executes the BulkDeleteAsync operation.
    /// </summary>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="ids">The ids value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the BulkDeleteAsync operation.</returns>
    public async ValueTask<int> BulkDeleteAsync(string tableName, IReadOnlyCollection<int> ids, string keyColumn = "Id", CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return 0;
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await ForgeSqlServerTvpBulkExecutor.DeleteAsync(c, tableName, keyColumn, ids, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes many rows using the provider-native bulk path. SQL Server uses key-only TVP + DELETE JOIN.</summary>
    public ValueTask<int> DeleteBulkAsync<T>(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default)
    {
        var metadata = _metadata.Resolve<T>();
        return DeleteBulkAsync(metadata.TableName, ids, metadata.KeyColumn, cancellationToken);
    }

    /// <summary>Deletes many rows using the provider-native bulk path. SQL Server uses key-only TVP + DELETE JOIN.</summary>
    public async ValueTask<int> DeleteBulkAsync(string tableName, IReadOnlyCollection<int> ids, string keyColumn = "Id", CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return 0;
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await ForgeSqlServerTvpBulkExecutor.DeleteAsync(c, tableName, keyColumn, ids, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <returns>The result of the T operation.</returns>
    public void BulkMerge<T>(IReadOnlyCollection<T> rows, string keyColumn = "Id") => BulkMergeAsync(rows, keyColumn).GetAwaiter().GetResult();
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public async ValueTask BulkMergeAsync<T>(IReadOnlyCollection<T> rows, string keyColumn = "Id", CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0) return;
        await UpdateBulkAsync(rows, keyColumn, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the BeginTransaction operation.
    /// </summary>
    /// <returns>The result of the BeginTransaction operation.</returns>
    public IForgeTransaction BeginTransaction()
    {
        var c = CreateConnection();
        c.Open();
        return ForgeTransaction.Begin(c);
    }

    /// <summary>
    /// Executes the BeginTransactionAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the BeginTransactionAsync operation.</returns>
    public async ValueTask<IForgeTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await ForgeTransaction.BeginAsync(c, cancellationToken);
    }

    /// <summary>
    /// Executes the Analyze operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the Analyze operation.</returns>
    public  ForgeORM.Abstractions.ForgeQueryAnalysis Analyze(string sql) => _analyzer.Analyze(sql);
}
