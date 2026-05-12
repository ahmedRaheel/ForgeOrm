using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using Dapper;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

internal sealed class ForgeGridReader : IForgeGridReader
{
    private readonly DbConnection _connection;
    private readonly SqlMapper.GridReader _reader;
    public ForgeGridReader(DbConnection connection, SqlMapper.GridReader reader) { _connection = connection; _reader = reader; }
    public IEnumerable<T> Read<T>() => _reader.Read<T>().ToList();
    public async Task<IReadOnlyList<T>> ReadAsync<T>() => (await _reader.ReadAsync<T>()).ToList();
    public void Dispose() { _reader.Dispose(); _connection.Dispose(); }
}

public sealed class ReflectionForgeEntityMetadataResolver : IForgeEntityMetadataResolver
{
    private readonly Dictionary<Type, ForgeEntityMetadata> _cache = [];
    public ForgeEntityMetadata Resolve<T>() => Resolve(typeof(T));
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

    public ForgeQuery(IForgeDb db, ForgeEntityMetadata meta, string? baseSql = null, object? parameters = null)
    {
        _db = db; _meta = meta; _baseSql = baseSql; _parameters = parameters;
    }

    public IForgeQuery<T> Where(Expression<Func<T, bool>> predicate) { _where.Add(ForgeExpressionTranslator.Translate(predicate)); return this; }
    public IForgeQuery<T> Where(string condition, object? parameters = null) { _where.Add(condition); return this; }
    public IForgeQuery<T> OrderBy(Expression<Func<T, object>> keySelector) { _orderBy = ForgeExpressionTranslator.MemberName(keySelector) + " ASC"; return this; }
    public IForgeQuery<T> OrderByDescending(Expression<Func<T, object>> keySelector) { _orderBy = ForgeExpressionTranslator.MemberName(keySelector) + " DESC"; return this; }
    public IForgeQuery<T> OrderBy(string orderBy) { _orderBy = orderBy; return this; }
    public IForgeQuery<T> Skip(int count) { _skip = count; return this; }
    public IForgeQuery<T> Take(int count) { _take = count; return this; }
    public IReadOnlyList<T> ToList() => _db.Query<T>(BuildSql(), _parameters).ToList();
    public Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default) => _db.QueryAsync<T>(BuildSql(), _parameters, cancellationToken: cancellationToken);
    public T? FirstOrDefault() { Take(1); return _db.QueryFirstOrDefault<T>(BuildSql(), _parameters); }
    public Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default) { Take(1); return _db.QueryFirstOrDefaultAsync<T>(BuildSql(), _parameters, cancellationToken: cancellationToken); }
    public int Count() => _db.ExecuteScalar<int>("SELECT COUNT(1) FROM (" + BuildBaseSql() + ") ForgeCount");
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
    public static string Translate<T>(Expression<Func<T, bool>> expression)
    {
        if (expression.Body is not BinaryExpression b) throw new NotSupportedException("Only simple binary expressions are supported in MVP.");
        return $"{Member(b.Left)} {Operator(b.NodeType)} {Value(b.Right)}";
    }
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
    public ForgeSplitQuery(IForgeDb db) => _db = db;

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

    public IReadOnlyList<TParent> ToList(string parentSql, object? parameters = null) => ToListAsync(parentSql, parameters).GetAwaiter().GetResult();
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

    public static ForgeTransaction Begin(DbConnection connection) => new(connection, connection.BeginTransaction());
    public static async Task<ForgeTransaction> BeginAsync(DbConnection connection, CancellationToken ct) => new(connection, await connection.BeginTransactionAsync(ct));

    public IEnumerable<T> Query<T>(string sql, object? parameters = null, int? timeoutSeconds = null) => _connection.Query<T>(sql, parameters, _transaction, false, timeoutSeconds).ToList();
    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default) => (await _connection.QueryAsync<T>(new CommandDefinition(sql, parameters, _transaction, timeoutSeconds, cancellationToken: cancellationToken))).ToList();
    public int Execute(string sql, object? parameters = null, int? timeoutSeconds = null) => _connection.Execute(sql, parameters, _transaction, timeoutSeconds);
    public Task<int> ExecuteAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default) => _connection.ExecuteAsync(new CommandDefinition(sql, parameters, _transaction, timeoutSeconds, cancellationToken: cancellationToken));
    public T? ExecuteScalar<T>(string sql, object? parameters = null, int? timeoutSeconds = null) => _connection.ExecuteScalar<T>(sql, parameters, _transaction, timeoutSeconds);
    public Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default) => _connection.ExecuteScalarAsync<T>(new CommandDefinition(sql, parameters, _transaction, timeoutSeconds, cancellationToken: cancellationToken));
    public void Commit() => _transaction.Commit();
    public Task CommitAsync(CancellationToken cancellationToken = default) => _transaction.CommitAsync(cancellationToken);
    public void Rollback() => _transaction.Rollback();
    public Task RollbackAsync(CancellationToken cancellationToken = default) => _transaction.RollbackAsync(cancellationToken);
    public void Dispose() { _transaction.Dispose(); _connection.Dispose(); }
    public async ValueTask DisposeAsync() { await _transaction.DisposeAsync(); await _connection.DisposeAsync(); }
}
