using System.Data.Common;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    public T? GetById<T>(object id) { var c = Provider.BuildGetById(_metadata.Resolve<T>(), id); return QuerySingleOrDefault<T>(c.CommandText, c.Parameters); }
    public Task<T?> GetByIdAsync<T>(object id, CancellationToken cancellationToken = default) { var c = Provider.BuildGetById(_metadata.Resolve<T>(), id); return QuerySingleOrDefaultAsync<T>(c.CommandText, c.Parameters, cancellationToken: cancellationToken); }
    public T? GetByCode<T>(string code) { var c = Provider.BuildGetByCode(_metadata.Resolve<T>(), code); return QuerySingleOrDefault<T>(c.CommandText, c.Parameters); }
    public Task<T?> GetByCodeAsync<T>(string code, CancellationToken cancellationToken = default) { var c = Provider.BuildGetByCode(_metadata.Resolve<T>(), code); return QuerySingleOrDefaultAsync<T>(c.CommandText, c.Parameters, cancellationToken: cancellationToken); }
    public IReadOnlyList<T> GetByIds<T>(IReadOnlyCollection<int> ids) { if (ids.Count == 0) return []; var c = Provider.BuildGetByIds(_metadata.Resolve<T>(), ids); return Query<T>(c.CommandText, c.Parameters).ToList(); }
    public Task<IReadOnlyList<T>> GetByIdsAsync<T>(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default) { if (ids.Count == 0) return Task.FromResult<IReadOnlyList<T>>([]); var c = Provider.BuildGetByIds(_metadata.Resolve<T>(), ids); return QueryAsync<T>(c.CommandText, c.Parameters, cancellationToken: cancellationToken); }
    public int Insert<T>(T entity) { var c = Provider.BuildInsert(_metadata.Resolve<T>(), entity!); return Execute(c.CommandText, c.Parameters); }
    public Task<int> InsertAsync<T>(T entity, CancellationToken cancellationToken = default) { var c = Provider.BuildInsert(_metadata.Resolve<T>(), entity!); return ExecuteAsync(c.CommandText, c.Parameters, cancellationToken: cancellationToken); }
    public int Update<T>(T entity) { var c = Provider.BuildUpdate(_metadata.Resolve<T>(), entity!); return Execute(c.CommandText, c.Parameters); }
    public Task<int> UpdateAsync<T>(T entity, CancellationToken cancellationToken = default) { var c = Provider.BuildUpdate(_metadata.Resolve<T>(), entity!); return ExecuteAsync(c.CommandText, c.Parameters, cancellationToken: cancellationToken); }
    public int Delete<T>(object id) { var c = Provider.BuildDelete(_metadata.Resolve<T>(), id); return Execute(c.CommandText, c.Parameters); }
    public Task<int> DeleteAsync<T>(object id, CancellationToken cancellationToken = default) { var c = Provider.BuildDelete(_metadata.Resolve<T>(), id); return ExecuteAsync(c.CommandText, c.Parameters, cancellationToken: cancellationToken); }

    public ForgePagedResult<T> Page<T>(ForgePageRequest request)
    {
        var count = Provider.BuildCount(request.Sql, request.Parameters);
        var total = ExecuteScalar<int>(count.CommandText, count.Parameters);
        var page = Provider.BuildPage(request);
        var rows = Query<T>(page.CommandText, page.Parameters).ToList();
        return new ForgePagedResult<T> { Items = rows, Page = request.Page, PageSize = request.PageSize, TotalRecords = total };
    }

    public async Task<ForgePagedResult<T>> PageAsync<T>(ForgePageRequest request, CancellationToken cancellationToken = default)
    {
        var count = Provider.BuildCount(request.Sql, request.Parameters);
        var total = await ExecuteScalarAsync<int>(count.CommandText, count.Parameters, cancellationToken: cancellationToken);
        var page = Provider.BuildPage(request);
        var rows = await QueryAsync<T>(page.CommandText, page.Parameters, cancellationToken: cancellationToken);
        return new ForgePagedResult<T> { Items = rows, Page = request.Page, PageSize = request.PageSize, TotalRecords = total };
    }

    public IForgeQuery<T> Set<T>() => new ForgeQuery<T>(this, _metadata.Resolve<T>());
    public IForgeQuery<T> From<T>() => Set<T>();
    public IForgeQuery<T> Sql<T>(string sql, object? parameters = null) => new ForgeQuery<T>(this, _metadata.Resolve<T>(), sql, parameters);

    public IForgeSplitQuery<TParent> Split<TParent>() => new ForgeSplitQuery<TParent>(this);

    public void BulkInsert<T>(IReadOnlyCollection<T> rows) => BulkInsert(_metadata.Resolve<T>().TableName, rows);
    public void BulkInsert<T>(string tableName, IReadOnlyCollection<T> rows) => BulkInsertAsync(tableName, rows).GetAwaiter().GetResult();
    public Task BulkInsertAsync<T>(IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default) => BulkInsertAsync(_metadata.Resolve<T>().TableName, rows, cancellationToken);
    public async Task BulkInsertAsync<T>(string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0) return;
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        await Provider.BulkInsertAsync(c, tableName, rows, cancellationToken);
    }

    public void BulkUpdate<T>(IReadOnlyCollection<T> rows, string keyColumn = "Id") => BulkUpdate(_metadata.Resolve<T>().TableName, rows, keyColumn);
    public void BulkUpdate<T>(string tableName, IReadOnlyCollection<T> rows, string keyColumn = "Id") => BulkUpdateAsync(tableName, rows, keyColumn).GetAwaiter().GetResult();
    public Task BulkUpdateAsync<T>(IReadOnlyCollection<T> rows, string keyColumn = "Id", CancellationToken cancellationToken = default) => BulkUpdateAsync(_metadata.Resolve<T>().TableName, rows, keyColumn, cancellationToken);
    public async Task BulkUpdateAsync<T>(string tableName, IReadOnlyCollection<T> rows, string keyColumn = "Id", CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0) return;
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        await Provider.BulkUpdateAsync(c, tableName, rows, keyColumn, cancellationToken);
    }

    public void BulkDelete<T>(IReadOnlyCollection<int> ids) => BulkDelete(_metadata.Resolve<T>().TableName, ids, _metadata.Resolve<T>().KeyColumn);
    public void BulkDelete(string tableName, IReadOnlyCollection<int> ids, string keyColumn = "Id") => BulkDeleteAsync(tableName, ids, keyColumn).GetAwaiter().GetResult();
    public Task BulkDeleteAsync<T>(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default) { var m = _metadata.Resolve<T>(); return BulkDeleteAsync(m.TableName, ids, m.KeyColumn, cancellationToken); }
    public Task BulkDeleteAsync(string tableName, IReadOnlyCollection<int> ids, string keyColumn = "Id", CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return Task.CompletedTask;
        var cmd = Provider.BuildBulkDelete(tableName, keyColumn, ids);
        return ExecuteAsync(cmd.CommandText, cmd.Parameters, cancellationToken: cancellationToken);
    }

    public void BulkMerge<T>(IReadOnlyCollection<T> rows, string keyColumn = "Id") => BulkMergeAsync(rows, keyColumn).GetAwaiter().GetResult();
    public async Task BulkMergeAsync<T>(IReadOnlyCollection<T> rows, string keyColumn = "Id", CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0) return;
        var table = _metadata.Resolve<T>().TableName;
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        await Provider.BulkMergeAsync(c, table, rows, keyColumn, cancellationToken);
    }

    public IForgeTransaction BeginTransaction()
    {
        var c = CreateConnection();
        c.Open();
        return ForgeTransaction.Begin(c);
    }

    public async Task<IForgeTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await ForgeTransaction.BeginAsync(c, cancellationToken);
    }

    public ForgeQueryAnalysis Analyze(string sql) => _analyzer.Analyze(sql);
}
