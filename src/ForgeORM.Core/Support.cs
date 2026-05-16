using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

internal sealed class ForgeGridReader : IForgeGridReader
{
    private readonly DbConnection _connection;
    private readonly DbCommand _command;
    private readonly DbDataReader _reader;
    private bool _hasConsumedCurrentResult;

    /// <summary>
    /// Initializes or executes the ForgeGridReader operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="command">The command value.</param>
    /// <param name="reader">The reader value.</param>
    public ForgeGridReader(DbConnection connection, DbCommand command, DbDataReader reader)
    {
        _connection = connection;
        _command = command;
        _reader = reader;
    }

    /// <summary>
    /// Initializes or executes the Read operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public IEnumerable<T> Read<T>() => ReadAsync<T>().GetAwaiter().GetResult();

    /// <summary>
    /// Initializes or executes the ReadAsync operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public async Task<IReadOnlyList<T>> ReadAsync<T>()
    {
        if (_hasConsumedCurrentResult)
            await _reader.NextResultAsync();

        var rows = new List<T>();
        while (await _reader.ReadAsync())
            rows.Add(ForgeMaterializer.Map<T>(_reader));

        _hasConsumedCurrentResult = true;
        return rows;
    }

    /// <summary>
    /// Initializes or executes the Dispose operation.
    /// </summary>
    public void Dispose()
    {
        _reader.Dispose();
        _command.Dispose();
        _connection.Dispose();
    }
}

public sealed class ReflectionForgeEntityMetadataResolver : IForgeEntityMetadataResolver
{
    private readonly Dictionary<Type, ForgeEntityMetadata> _cache = [];
    /// <summary>
    /// Initializes or executes the Resolve operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public ForgeEntityMetadata Resolve<T>() => Resolve(typeof(T));
    /// <summary>
    /// Initializes or executes the Resolve operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The operation result.</returns>
    public ForgeEntityMetadata Resolve(Type type)
    {
        if (_cache.TryGetValue(type, out var cached)) return cached;
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead).Select(p => new ForgePropertyMetadata
        {
            PropertyName = p.Name,
            ColumnName = p.GetCustomAttribute<ForgeColumnAttribute>()?.Name ?? p.Name,
            PropertyType = p.PropertyType,
            IsKey = p.GetCustomAttribute<ForgeKeyAttribute>() is not null || p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase),
            IsCode = p.GetCustomAttribute<ForgeCodeAttribute>() is not null || p.Name.Equals("Code", StringComparison.OrdinalIgnoreCase),
            IsComputed = p.GetCustomAttribute<ForgeComputedAttribute>() is not null
        }).ToList();
        var meta = new ForgeEntityMetadata
        {
            EntityType = type,
            TableName = type.GetCustomAttribute<ForgeTableAttribute>()?.Name ?? type.Name,
            KeyColumn = props.FirstOrDefault(x => x.IsKey)?.ColumnName ?? "Id",
            CodeColumn = props.FirstOrDefault(x => x.IsCode)?.ColumnName ?? "Code",
            Properties = props
        };
        _cache[type] = meta;
        return meta;
    }
}

internal sealed class ForgeQuery<T> : IForgeQuery<T>
{
    private readonly IForgeDb _db;
    private readonly ForgeEntityMetadata _meta;
    private readonly string? _baseSql;
    private readonly object? _parameters;
    private readonly List<string> _where = [];
    private string? _orderBy;
    private int? _skip;
    private int? _take;

    /// <summary>
    /// Initializes or executes the ForgeQuery operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    /// <param name="meta">The meta value.</param>
    /// <param name="baseSql">The baseSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    public ForgeQuery(IForgeDb db, ForgeEntityMetadata meta, string? baseSql = null, object? parameters = null)
    {
        _db = db; _meta = meta; _baseSql = baseSql; _parameters = parameters;
    }

    /// <summary>
    /// Initializes or executes the Where operation.
    /// </summary>
    /// <param name="_where">The _where value.</param>
    /// <returns>The operation result.</returns>
    public IForgeQuery<T> Where(Expression<Func<T, bool>> predicate) { _where.Add(ForgeExpressionTranslator.Translate(predicate)); return this; }
    /// <summary>
    /// Initializes or executes the Where operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The operation result.</returns>
    public IForgeQuery<T> Where(string condition, object? parameters = null) { _where.Add(condition); return this; }
    /// <summary>
    /// Initializes or executes the OrderBy operation.
    /// </summary>
    /// <param name="_orderBy">The _orderBy value.</param>
    /// <returns>The operation result.</returns>
    public IForgeQuery<T> OrderBy(Expression<Func<T, object>> keySelector) { _orderBy = ForgeExpressionTranslator.MemberName(keySelector) + " ASC"; return this; }
    /// <summary>
    /// Initializes or executes the OrderByDescending operation.
    /// </summary>
    /// <param name="_orderBy">The _orderBy value.</param>
    /// <returns>The operation result.</returns>
    public IForgeQuery<T> OrderByDescending(Expression<Func<T, object>> keySelector) { _orderBy = ForgeExpressionTranslator.MemberName(keySelector) + " DESC"; return this; }
    /// <summary>
    /// Initializes or executes the OrderBy operation.
    /// </summary>
    /// <param name="orderBy">The orderBy value.</param>
    /// <returns>The operation result.</returns>
    public IForgeQuery<T> OrderBy(string orderBy) { _orderBy = orderBy; return this; }
    /// <summary>
    /// Initializes or executes the Skip operation.
    /// </summary>
    /// <param name="count">The count value.</param>
    /// <returns>The operation result.</returns>
    public IForgeQuery<T> Skip(int count) { _skip = count; return this; }
    /// <summary>
    /// Initializes or executes the Take operation.
    /// </summary>
    /// <param name="count">The count value.</param>
    /// <returns>The operation result.</returns>
    public IForgeQuery<T> Take(int count) { _take = count; return this; }
    /// <summary>
    /// Initializes or executes the ToList operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public IReadOnlyList<T> ToList() => _db.Query<T>(BuildSql(), _parameters).ToList();
    /// <summary>
    /// Initializes or executes the ToListAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default) => _db.QueryAsync<T>(BuildSql(), _parameters, cancellationToken: cancellationToken);
    /// <summary>
    /// Initializes or executes the FirstOrDefault operation.
    /// </summary>
    /// <param name="_parameters">The _parameters value.</param>
    /// <returns>The operation result.</returns>
    public T? FirstOrDefault() { Take(1); return _db.QueryFirstOrDefault<T>(BuildSql(), _parameters); }
    /// <summary>
    /// Initializes or executes the FirstOrDefaultAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default) { Take(1); return _db.QueryFirstOrDefaultAsync<T>(BuildSql(), _parameters, cancellationToken: cancellationToken); }
    /// <summary>
    /// Initializes or executes the Count operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public int Count() => _db.ExecuteScalar<int>("SELECT COUNT(1) FROM (" + BuildBaseSql() + ") ForgeCount");
    /// <summary>
    /// Initializes or executes the CountAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public async Task<int> CountAsync(CancellationToken cancellationToken = default) => await _db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM (" + BuildBaseSql() + ") ForgeCount", cancellationToken: cancellationToken);

    private string BuildSql()
    {
        var sql = BuildBaseSql();
        if (!string.IsNullOrWhiteSpace(_orderBy)) sql += " ORDER BY " + _orderBy;
        if (_take.HasValue) sql += $" OFFSET {_skip ?? 0} ROWS FETCH NEXT {_take.Value} ROWS ONLY";
        return sql;
    }
    private string BuildBaseSql()
    {
        var sql = _baseSql ?? $"SELECT * FROM {_meta.TableName}";
        if (_where.Count > 0) sql += " WHERE " + string.Join(" AND ", _where);
        return sql;
    }
}

internal static class ForgeExpressionTranslator
{
    /// <summary>
    /// Initializes or executes the Translate operation.
    /// </summary>
    /// <param name="expression">The expression value.</param>
    /// <returns>The operation result.</returns>
    public static string Translate<T>(Expression<Func<T, bool>> expression)
    {
        if (expression.Body is not BinaryExpression b) throw new NotSupportedException("Only simple binary expressions are supported in MVP.");
        return $"{Member(b.Left)} {Operator(b.NodeType)} {Value(b.Right)}";
    }
    /// <summary>
    /// Initializes or executes the MemberName operation.
    /// </summary>
    /// <param name="expression">The expression value.</param>
    /// <returns>The operation result.</returns>
    public static string MemberName<T>(Expression<Func<T, object>> expression)
    {
        Expression body = expression.Body is UnaryExpression u ? u.Operand : expression.Body;
        return body is MemberExpression m ? m.Member.Name : throw new NotSupportedException("Only member expression is supported.");
    }
    private static string Member(Expression e) => e is MemberExpression m ? m.Member.Name : throw new NotSupportedException("Left side must be member.");
    private static string Operator(ExpressionType t) => t switch { ExpressionType.Equal => "=", ExpressionType.NotEqual => "<>", ExpressionType.GreaterThan => ">", ExpressionType.GreaterThanOrEqual => ">=", ExpressionType.LessThan => "<", ExpressionType.LessThanOrEqual => "<=", _ => throw new NotSupportedException("Operator not supported.") };
    private static string Value(Expression e)
    {
        var v = Expression.Lambda(e).Compile().DynamicInvoke();
        return v switch { null => "NULL", string s => "'" + s.Replace("'", "''") + "'", DateTime d => "'" + d.ToString("yyyy-MM-dd HH:mm:ss") + "'", bool b => b ? "1" : "0", _ => v?.ToString() ?? "NULL" };
    }
}

internal sealed class ForgeSplitQuery<TParent> : IForgeSplitQuery<TParent>
{
    private readonly IForgeDb _db;
    private readonly List<Func<IReadOnlyList<TParent>, CancellationToken, Task>> _includes = [];
    /// <summary>
    /// Initializes or executes the ForgeSplitQuery operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    public ForgeSplitQuery(IForgeDb db) => _db = db;

    /// <summary>
    /// Initializes or executes the TKey> operation.
    /// </summary>
    /// <param name="childSqlFactory">The childSqlFactory value.</param>
    /// <param name="parentKey">The parentKey value.</param>
    /// <param name="childForeignKey">The childForeignKey value.</param>
    /// <param name="assign">The assign value.</param>
    /// <returns>The operation result.</returns>
    public IForgeSplitQuery<TParent> IncludeMany<TChild, TKey>(Func<IReadOnlyCollection<TKey>, string> childSqlFactory, Func<TParent, TKey> parentKey, Func<TChild, TKey> childForeignKey, Action<TParent, IReadOnlyList<TChild>> assign) where TKey : notnull
    {
        _includes.Add(async (parents, ct) =>
        {
            var ids = parents.Select(parentKey).Distinct().ToList();
            if (ids.Count == 0) return;
            var children = await _db.QueryAsync<TChild>(childSqlFactory(ids), new { Ids = ids }, cancellationToken: ct);
            var lookup = children.GroupBy(childForeignKey).ToDictionary(x => x.Key, x => (IReadOnlyList<TChild>)x.ToList());
            foreach (var p in parents) assign(p, lookup.TryGetValue(parentKey(p), out var rows) ? rows : []);
        });
        return this;
    }

    /// <summary>
    /// Initializes or executes the ToList operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The operation result.</returns>
    public IReadOnlyList<TParent> ToList(string parentSql, object? parameters = null) => ToListAsync(parentSql, parameters).GetAwaiter().GetResult();
    /// <summary>
    /// Initializes or executes the ToListAsync operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public async Task<IReadOnlyList<TParent>> ToListAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        var parents = (await _db.QueryAsync<TParent>(parentSql, parameters, cancellationToken: cancellationToken)).ToList();
        foreach (var include in _includes) await include(parents, cancellationToken);
        return parents;
    }
}

internal sealed class ForgeTransaction : IForgeTransaction
{
    private readonly DbConnection _connection;
    private readonly DbTransaction _transaction;

    private ForgeTransaction(DbConnection connection, DbTransaction transaction)
    {
        _connection = connection; _transaction = transaction;
    }

    /// <summary>
    /// Initializes or executes the Begin operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <returns>The operation result.</returns>
    public static ForgeTransaction Begin(DbConnection connection) => new(connection, connection.BeginTransaction());
    /// <summary>
    /// Initializes or executes the BeginAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="ct">The ct value.</param>
    /// <returns>The operation result.</returns>
    public static async Task<ForgeTransaction> BeginAsync(DbConnection connection, CancellationToken ct) => new(connection, await connection.BeginTransactionAsync(ct));

    /// <summary>
    /// Initializes or executes the Query operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public IEnumerable<T> Query<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
        => ForgeAdo.Query<T>(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds).ToList();

    /// <summary>
    /// Initializes or executes the QueryAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken  = default)
        => ForgeAdo.QueryAsync<T>(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);

    /// <summary>
    /// Initializes or executes the Execute operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public int Execute(string sql, object? parameters = null, int? timeoutSeconds = null)
        => ForgeAdo.Execute(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds);

    /// <summary>
    /// Initializes or executes the ExecuteAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public Task<int> ExecuteAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgeAdo.ExecuteAsync(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);

    /// <summary>
    /// Initializes or executes the ExecuteScalar operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public T? ExecuteScalar<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
        => ForgeAdo.ExecuteScalar<T>(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds);

    /// <summary>
    /// Initializes or executes the ExecuteScalarAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgeAdo.ExecuteScalarAsync<T>(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);
    /// <summary>
    /// Initializes or executes the Commit operation.
    /// </summary>
    public void Commit() => _transaction.Commit();
    /// <summary>
    /// Initializes or executes the CommitAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public Task CommitAsync(CancellationToken cancellationToken = default) => _transaction.CommitAsync(cancellationToken);
    /// <summary>
    /// Initializes or executes the Rollback operation.
    /// </summary>
    public void Rollback() => _transaction.Rollback();
    /// <summary>
    /// Initializes or executes the RollbackAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public Task RollbackAsync(CancellationToken cancellationToken = default) => _transaction.RollbackAsync(cancellationToken);
    /// <summary>
    /// Initializes or executes the Dispose operation.
    /// </summary>
    /// <param name="_connection">The _connection value.</param>
    public void Dispose() { _transaction.Dispose(); _connection.Dispose(); }
    /// <summary>
    /// Initializes or executes the DisposeAsync operation.
    /// </summary>
    /// <param name="_connection">The _connection value.</param>
    /// <returns>The operation result.</returns>
    public async ValueTask DisposeAsync() { await _transaction.DisposeAsync(); await _connection.DisposeAsync(); }
}
