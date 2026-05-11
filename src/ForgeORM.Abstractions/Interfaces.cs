using System.Data.Common;
using System.Linq.Expressions;

namespace ForgeORM.Abstractions;

public interface IForgeDb :
    IForgeRawSql,
    IForgeStoredProcedures,
    IForgeDatabaseFunctions,
    IForgeRepository,
    IForgeQueryableFactory,
    IForgeSplitQueryFactory,
    IForgeBulkOperations,
    IForgeTransactionManager,
    IForgeDiagnostics
{
    IForgeDatabaseProvider Provider { get; }
}

public interface IForgeRawSql
{
    IEnumerable<T> Query<T>(string sql, object? parameters = null, int? timeoutSeconds = null);
    Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    T QueryFirst<T>(string sql, object? parameters = null, int? timeoutSeconds = null);
    Task<T> QueryFirstAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    T? QueryFirstOrDefault<T>(string sql, object? parameters = null, int? timeoutSeconds = null);
    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    T QuerySingle<T>(string sql, object? parameters = null, int? timeoutSeconds = null);
    Task<T> QuerySingleAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    T? QuerySingleOrDefault<T>(string sql, object? parameters = null, int? timeoutSeconds = null);
    Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    int Execute(string sql, object? parameters = null, int? timeoutSeconds = null);
    Task<int> ExecuteAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    T? ExecuteScalar<T>(string sql, object? parameters = null, int? timeoutSeconds = null);
    Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    IForgeGridReader QueryMultiple(string sql, object? parameters = null, int? timeoutSeconds = null);
    Task<IForgeGridReader> QueryMultipleAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
}

public interface IForgeStoredProcedures
{
    IEnumerable<T> QueryProcedure<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null);
    Task<IReadOnlyList<T>> QueryProcedureAsync<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    T? QueryProcedureSingleOrDefault<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null);
    Task<T?> QueryProcedureSingleOrDefaultAsync<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    int ExecuteProcedure(string procedureName, object? parameters = null, int? timeoutSeconds = null);
    Task<int> ExecuteProcedureAsync(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    T? ExecuteProcedureScalar<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null);
    Task<T?> ExecuteProcedureScalarAsync<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    IForgeGridReader QueryProcedureMultiple(string procedureName, object? parameters = null, int? timeoutSeconds = null);
    Task<IForgeGridReader> QueryProcedureMultipleAsync(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
}

public interface IForgeDatabaseFunctions
{
    T? ExecuteFunction<T>(string functionName, object? parameters = null, int? timeoutSeconds = null);
    Task<T?> ExecuteFunctionAsync<T>(string functionName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    IReadOnlyList<T> QueryFunction<T>(string functionSql, object? parameters = null, int? timeoutSeconds = null);
    Task<IReadOnlyList<T>> QueryFunctionAsync<T>(string functionSql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
}

public interface IForgeRepository
{
    T? GetById<T>(object id);
    Task<T?> GetByIdAsync<T>(object id, CancellationToken cancellationToken = default);
    T? GetByCode<T>(string code);
    Task<T?> GetByCodeAsync<T>(string code, CancellationToken cancellationToken = default);
    IReadOnlyList<T> GetByIds<T>(IReadOnlyCollection<int> ids);
    Task<IReadOnlyList<T>> GetByIdsAsync<T>(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);
    int Insert<T>(T entity);
    Task<int> InsertAsync<T>(T entity, CancellationToken cancellationToken = default);
    int Update<T>(T entity);
    Task<int> UpdateAsync<T>(T entity, CancellationToken cancellationToken = default);
    int Delete<T>(object id);
    Task<int> DeleteAsync<T>(object id, CancellationToken cancellationToken = default);
    ForgePagedResult<T> Page<T>(ForgePageRequest request);
    Task<ForgePagedResult<T>> PageAsync<T>(ForgePageRequest request, CancellationToken cancellationToken = default);
}

public interface IForgeQueryableFactory
{
    IForgeQuery<T> Set<T>();
    IForgeQuery<T> From<T>();
    IForgeQuery<T> Sql<T>(string sql, object? parameters = null);
}

public interface IForgeQuery<T>
{
    IForgeQuery<T> Where(Expression<Func<T, bool>> predicate);
    IForgeQuery<T> Where(string condition, object? parameters = null);
    IForgeQuery<T> OrderBy(Expression<Func<T, object>> keySelector);
    IForgeQuery<T> OrderByDescending(Expression<Func<T, object>> keySelector);
    IForgeQuery<T> OrderBy(string orderBy);
    IForgeQuery<T> Skip(int count);
    IForgeQuery<T> Take(int count);
    IReadOnlyList<T> ToList();
    Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default);
    T? FirstOrDefault();
    Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);
    int Count();
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

public interface IForgeSplitQueryFactory
{
    IForgeSplitQuery<TParent> Split<TParent>();
}

public interface IForgeSplitQuery<TParent>
{
    IForgeSplitQuery<TParent> IncludeMany<TChild, TKey>(
        Func<IReadOnlyCollection<TKey>, string> childSqlFactory,
        Func<TParent, TKey> parentKey,
        Func<TChild, TKey> childForeignKey,
        Action<TParent, IReadOnlyList<TChild>> assign)
        where TKey : notnull;

    IReadOnlyList<TParent> ToList(string parentSql, object? parameters = null);
    Task<IReadOnlyList<TParent>> ToListAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default);
}

public interface IForgeBulkOperations
{
    void BulkInsert<T>(IReadOnlyCollection<T> rows);
    void BulkInsert<T>(string tableName, IReadOnlyCollection<T> rows);
    Task BulkInsertAsync<T>(IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default);
    Task BulkInsertAsync<T>(string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default);
    void BulkUpdate<T>(IReadOnlyCollection<T> rows, string keyColumn = "Id");
    void BulkUpdate<T>(string tableName, IReadOnlyCollection<T> rows, string keyColumn = "Id");
    Task BulkUpdateAsync<T>(IReadOnlyCollection<T> rows, string keyColumn = "Id", CancellationToken cancellationToken = default);
    Task BulkUpdateAsync<T>(string tableName, IReadOnlyCollection<T> rows, string keyColumn = "Id", CancellationToken cancellationToken = default);
    void BulkDelete<T>(IReadOnlyCollection<int> ids);
    void BulkDelete(string tableName, IReadOnlyCollection<int> ids, string keyColumn = "Id");
    Task BulkDeleteAsync<T>(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);
    Task BulkDeleteAsync(string tableName, IReadOnlyCollection<int> ids, string keyColumn = "Id", CancellationToken cancellationToken = default);
    void BulkMerge<T>(IReadOnlyCollection<T> rows, string keyColumn = "Id");
    Task BulkMergeAsync<T>(IReadOnlyCollection<T> rows, string keyColumn = "Id", CancellationToken cancellationToken = default);
}

public interface IForgeTransactionManager
{
    IForgeTransaction BeginTransaction();
    Task<IForgeTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

public interface IForgeTransaction : IDisposable, IAsyncDisposable
{
    IEnumerable<T> Query<T>(string sql, object? parameters = null, int? timeoutSeconds = null);
    Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    int Execute(string sql, object? parameters = null, int? timeoutSeconds = null);
    Task<int> ExecuteAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    T? ExecuteScalar<T>(string sql, object? parameters = null, int? timeoutSeconds = null);
    Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    void Commit();
    Task CommitAsync(CancellationToken cancellationToken = default);
    void Rollback();
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

public interface IForgeGridReader : IDisposable
{
    IEnumerable<T> Read<T>();
    Task<IReadOnlyList<T>> ReadAsync<T>();
}

public interface IForgeDiagnostics
{
    ForgeQueryAnalysis Analyze(string sql);
}

public interface IForgeQueryAnalyzer
{
    ForgeQueryAnalysis Analyze(string sql);
}

public interface IForgeEntityMetadataResolver
{
    ForgeEntityMetadata Resolve<T>();
    ForgeEntityMetadata Resolve(Type type);
}

public interface IForgeDatabaseProvider
{
    string ProviderName { get; }
    ForgeSqlDialect Dialect { get; }
    ForgeProviderCapabilities Capabilities { get; }

    DbConnection CreateConnection(string connectionString);
    ForgeCommand BuildGetById(ForgeEntityMetadata entity, object id);
    ForgeCommand BuildGetByCode(ForgeEntityMetadata entity, string code);
    ForgeCommand BuildGetByIds(ForgeEntityMetadata entity, IReadOnlyCollection<int> ids);
    ForgeCommand BuildInsert(ForgeEntityMetadata entity, object entityInstance);
    ForgeCommand BuildUpdate(ForgeEntityMetadata entity, object entityInstance);
    ForgeCommand BuildDelete(ForgeEntityMetadata entity, object id);
    ForgeCommand BuildPage(ForgePageRequest request);
    ForgeCommand BuildCount(string baseSql, object? parameters = null);
    ForgeCommand BuildBulkDelete(string tableName, string keyColumn, IReadOnlyCollection<int> ids);
    ForgeCommand BuildFunctionScalar(string functionName, object? parameters = null);
    Task BulkInsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default);
    Task BulkUpdateAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default);
    Task BulkMergeAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default);
}
