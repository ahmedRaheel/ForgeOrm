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
    /// Executes the ForgeGridReader operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="command">The command value.</param>
    /// <param name="reader">The reader value.</param>
    /// <returns>The result of the ForgeGridReader operation.</returns>
    public ForgeGridReader(DbConnection connection, DbCommand command, DbDataReader reader)
    {
        _connection = connection;
        _command = command;
        _reader = reader;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    public IEnumerable<T> Read<T>() => ReadAsync<T>().GetAwaiter().GetResult();

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
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
    /// Executes the Dispose operation.
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
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    public ForgeEntityMetadata Resolve<T>() => Resolve(typeof(T));
    /// <summary>
    /// Executes the Resolve operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the Resolve operation.</returns>
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
    private readonly object? _baseParameters;
    private readonly List<string> _where = [];
    private readonly Dictionary<string, object?> _parameters = new(StringComparer.OrdinalIgnoreCase);
    private string? _orderBy;
    private int? _skip;
    private int? _take;

    /// <summary>
    /// Executes the ForgeQuery operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    /// <param name="meta">The meta value.</param>
    /// <param name="baseSql">The baseSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the ForgeQuery operation.</returns>
    public ForgeQuery(IForgeDb db, ForgeEntityMetadata meta, string? baseSql = null, object? parameters = null)
    {
        _db = db;
        _meta = meta;
        _baseSql = baseSql;
        _baseParameters = parameters;
        MergeParameters(parameters);
    }

    /// <summary>
    /// Executes the Where operation.
    /// </summary>
    /// <param name="predicate">The predicate value.</param>
    /// <returns>The result of the Where operation.</returns>
    public IForgeQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        _where.Add(ForgeExpressionTranslator.Translate(predicate));
        return this;
    }

    /// <summary>
    /// Executes the Where operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the Where operation.</returns>
    public IForgeQuery<T> Where(string condition, object? parameters = null) => WhereSql(condition, parameters);

    /// <summary>
    /// Executes the WhereSql operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the WhereSql operation.</returns>
    public IForgeQuery<T> WhereSql(string condition, object? parameters = null)
    {
        _where.Add(condition);
        MergeParameters(parameters);
        return this;
    }

    /// <summary>
    /// Executes the WhereIf operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="predicate">The predicate value.</param>
    /// <returns>The result of the WhereIf operation.</returns>
    public IForgeQuery<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate)
        => condition ? Where(predicate) : this;

    /// <summary>
    /// Executes the WhereSqlIf operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="sqlCondition">The sqlCondition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the WhereSqlIf operation.</returns>
    public IForgeQuery<T> WhereSqlIf(bool condition, string sqlCondition, object? parameters = null)
        => condition ? WhereSql(sqlCondition, parameters) : this;

    /// <summary>
    /// Executes the OrderBy operation.
    /// </summary>
    /// <param name="keySelector">The keySelector value.</param>
    /// <returns>The result of the OrderBy operation.</returns>
    public IForgeQuery<T> OrderBy(Expression<Func<T, object>> keySelector)
    {
        _orderBy = ForgeExpressionTranslator.MemberName(keySelector) + " ASC";
        return this;
    }

    /// <summary>
    /// Executes the OrderByDescending operation.
    /// </summary>
    /// <param name="keySelector">The keySelector value.</param>
    /// <returns>The result of the OrderByDescending operation.</returns>
    public IForgeQuery<T> OrderByDescending(Expression<Func<T, object>> keySelector)
    {
        _orderBy = ForgeExpressionTranslator.MemberName(keySelector) + " DESC";
        return this;
    }

    /// <summary>
    /// Executes the OrderBy operation.
    /// </summary>
    /// <param name="orderBy">The orderBy value.</param>
    /// <returns>The result of the OrderBy operation.</returns>
    public IForgeQuery<T> OrderBy(string orderBy) => OrderBySql(orderBy);

    /// <summary>
    /// Executes the OrderBySql operation.
    /// </summary>
    /// <param name="orderBy">The orderBy value.</param>
    /// <returns>The result of the OrderBySql operation.</returns>
    public IForgeQuery<T> OrderBySql(string orderBy)
    {
        _orderBy = orderBy;
        return this;
    }

    /// <summary>
    /// Executes the Skip operation.
    /// </summary>
    /// <param name="count">The count value.</param>
    /// <returns>The result of the Skip operation.</returns>
    public IForgeQuery<T> Skip(int count)
    {
        _skip = count;
        return this;
    }

    /// <summary>
    /// Executes the Take operation.
    /// </summary>
    /// <param name="count">The count value.</param>
    /// <returns>The result of the Take operation.</returns>
    public IForgeQuery<T> Take(int count)
    {
        _take = count;
        return this;
    }

    /// <summary>
    /// Executes the Any operation.
    /// </summary>
    /// <returns>The result of the Any operation.</returns>
    public bool Any() => _db.ExecuteScalar<int>(BuildAnySql(), BuildParameters()) > 0;

    /// <summary>
    /// Executes the AnyAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the AnyAsync operation.</returns>
    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => await _db.ExecuteScalarAsync<int>(BuildAnySql(), BuildParameters(), cancellationToken: cancellationToken) > 0;

    /// <summary>
    /// Executes the ToList operation.
    /// </summary>
    /// <returns>The result of the ToList operation.</returns>
    public IReadOnlyList<T> ToList() => _db.Query<T>(BuildSql(), BuildParameters()).ToList();

    /// <summary>
    /// Executes the ToListAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ToListAsync operation.</returns>
    public Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default)
        => _db.QueryAsync<T>(BuildSql(), BuildParameters(), cancellationToken: cancellationToken);

    /// <summary>
    /// Executes the FirstOrDefault operation.
    /// </summary>
    /// <returns>The result of the FirstOrDefault operation.</returns>
    public T? FirstOrDefault()
    {
        Take(1);
        return _db.QueryFirstOrDefault<T>(BuildSql(), BuildParameters());
    }

    /// <summary>
    /// Executes the FirstOrDefaultAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the FirstOrDefaultAsync operation.</returns>
    public Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        Take(1);
        return _db.QueryFirstOrDefaultAsync<T>(BuildSql(), BuildParameters(), cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Executes the Count operation.
    /// </summary>
    /// <returns>The result of the Count operation.</returns>
    public int Count() => _db.ExecuteScalar<int>(BuildCountSql(), BuildParameters());

    /// <summary>
    /// Executes the CountAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the CountAsync operation.</returns>
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        => await _db.ExecuteScalarAsync<int>(BuildCountSql(), BuildParameters(), cancellationToken: cancellationToken);

    /// <summary>Executes SUM for the selected decimal column.</summary>
    public Task<decimal> SumAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
        => ExecuteDecimalAggregateAsync("SUM", selector, cancellationToken);

    /// <summary>Executes AVG for the selected decimal column.</summary>
    public Task<decimal> AverageAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
        => ExecuteDecimalAggregateAsync("AVG", selector, cancellationToken);

    /// <summary>Executes MIN for the selected decimal column.</summary>
    public Task<decimal> MinAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
        => ExecuteDecimalAggregateAsync("MIN", selector, cancellationToken);

    /// <summary>Executes MAX for the selected decimal column.</summary>
    public Task<decimal> MaxAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
        => ExecuteDecimalAggregateAsync("MAX", selector, cancellationToken);

    /// <summary>Executes expression-based paging using the current query filters and ordering.</summary>
    public async Task<ForgePagedResult<T>> PageAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page <= 0) throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");
        if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

        var totalRecords = await CountAsync(cancellationToken);
        var previousSkip = _skip;
        var previousTake = _take;

        try
        {
            _skip = (page - 1) * pageSize;
            _take = pageSize;

            var items = await ToListAsync(cancellationToken);
            return new ForgePagedResult<T>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };
        }
        finally
        {
            _skip = previousSkip;
            _take = previousTake;
        }
    }

    private string BuildSql()
    {
        var sql = BuildBaseSql();
        if (!string.IsNullOrWhiteSpace(_orderBy))
        {
            sql += " ORDER BY " + _orderBy;
        }
        else if (_skip.HasValue || _take.HasValue)
        {
            sql += " ORDER BY 1";
        }

        if (_skip.HasValue || _take.HasValue)
        {
            sql += $" OFFSET {_skip ?? 0} ROWS";
            if (_take.HasValue)
            {
                sql += $" FETCH NEXT {_take.Value} ROWS ONLY";
            }
        }

        return sql;
    }

    private string BuildBaseSql()
    {
        var sql = _baseSql ?? $"SELECT * FROM {_meta.TableName}";
        if (_where.Count > 0) sql += " WHERE " + string.Join(" AND ", _where);
        return sql;
    }

    private string BuildCountSql() => "SELECT COUNT(1) FROM (" + BuildBaseSql() + ") ForgeCount";

    private string BuildAnySql() => "SELECT CASE WHEN EXISTS (" + BuildBaseSql() + ") THEN 1 ELSE 0 END";

    private string BuildAggregateSql(string function, LambdaExpression selector)
    {
        var column = ForgeExpressionTranslator.MemberName(selector);
        return $"SELECT COALESCE({function}({column}), 0) FROM (" + BuildBaseSql() + ") ForgeAggregate";
    }

    private async Task<decimal> ExecuteDecimalAggregateAsync(
        string function,
        Expression<Func<T, decimal>> selector,
        CancellationToken cancellationToken)
    {
        return await _db.ExecuteScalarAsync<decimal>(
            BuildAggregateSql(function, selector),
            BuildParameters(),
            cancellationToken: cancellationToken);
    }

    private object? BuildParameters() => _parameters.Count == 0 ? _baseParameters : _parameters;

    private void MergeParameters(object? parameters)
    {
        if (parameters is null) return;
        if (parameters is IReadOnlyDictionary<string, object?> readonlyDictionary)
        {
            foreach (var item in readonlyDictionary) _parameters[item.Key] = item.Value;
            return;
        }
        if (parameters is IDictionary<string, object?> dictionary)
        {
            foreach (var item in dictionary) _parameters[item.Key] = item.Value;
            return;
        }
        foreach (var property in parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanRead))
            _parameters[property.Name] = property.GetValue(parameters);
    }
}

internal static class ForgeExpressionTranslator
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="expression">The expression value.</param>
    /// <returns>The result of the T operation.</returns>
    public static string Translate<T>(Expression<Func<T, bool>> expression)
    {
        if (expression.Body is not BinaryExpression b) throw new NotSupportedException("Only simple binary expressions are supported in MVP.");
        return $"{Member(b.Left)} {Operator(b.NodeType)} {Value(b.Right)}";
    }
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="expression">The expression value.</param>
    /// <returns>The result of the T operation.</returns>
    public static string MemberName<T>(Expression<Func<T, object>> expression)
    {
        Expression body = expression.Body is UnaryExpression u ? u.Operand : expression.Body;
        return body is MemberExpression m ? m.Member.Name : throw new NotSupportedException("Only member expression is supported.");
    }

    public static string MemberName(LambdaExpression expression)
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
    /// Executes the ForgeSplitQuery operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    /// <returns>The result of the ForgeSplitQuery operation.</returns>
    public ForgeSplitQuery(IForgeDb db) => _db = db;

    /// <summary>
    /// Executes the TKey operation.
    /// </summary>
    /// <typeparam name="TChild">The type used by the operation.</typeparam>
    /// <typeparam name="TKey">The type used by the operation.</typeparam>
    /// <param name="childSqlFactory">The childSqlFactory value.</param>
    /// <param name="parentKey">The parentKey value.</param>
    /// <param name="childForeignKey">The childForeignKey value.</param>
    /// <param name="assign">The assign value.</param>
    /// <returns>The result of the TKey operation.</returns>
    public IForgeSplitQuery<TParent> IncludeMany<TChild, TKey>(
        Func<IReadOnlyCollection<TKey>, string> childSqlFactory,
        Func<TParent, TKey> parentKey,
        Func<TChild, TKey> childForeignKey,
        Action<TParent, IReadOnlyList<TChild>> assign)
        where TKey : notnull
    {
        _includes.Add(async (parents, ct) =>
        {
            var ids = parents.Select(parentKey).Distinct().ToList();
            if (ids.Count == 0) return;

            var children = await _db.QueryAsync<TChild>(childSqlFactory(ids), new { Ids = ids }, cancellationToken: ct);
            var lookup = children.GroupBy(childForeignKey).ToDictionary(x => x.Key, x => (IReadOnlyList<TChild>)x.ToList());

            foreach (var parent in parents)
                assign(parent, lookup.TryGetValue(parentKey(parent), out var rows) ? rows : Array.Empty<TChild>());
        });

        return this;
    }

    /// <summary>
    /// Executes the TChild operation.
    /// </summary>
    /// <typeparam name="TChild">The type used by the operation.</typeparam>
    /// <param name="childTable">The childTable value.</param>
    /// <param name="parentKey">The parentKey value.</param>
    /// <param name="childForeignKey">The childForeignKey value.</param>
    /// <param name="target">The target value.</param>
    /// <param name="backingField">The backingField value.</param>
    /// <param name="childWhereSql">The childWhereSql value.</param>
    /// <returns>The result of the TChild operation.</returns>
    public IForgeSplitQuery<TParent> IncludeMany<TChild>(
        string childTable,
        string parentKey = "Id",
        string childForeignKey = "ParentId",
        Expression<Func<TParent, IEnumerable<TChild>>>? target = null,
        string? backingField = null,
        string? childWhereSql = null)
    {
        var parentKeyProperty = FindProperty(typeof(TParent), parentKey);
        var childForeignKeyProperty = FindProperty(typeof(TChild), childForeignKey);

        _includes.Add(async (parents, ct) =>
        {
            var ids = parents
                .Select(x => parentKeyProperty.GetValue(x))
                .Where(x => x is not null)
                .Distinct()
                .ToList();

            if (ids.Count == 0) return;

            var sql = $"SELECT * FROM {childTable} WHERE {childForeignKey} IN @Ids";
            if (!string.IsNullOrWhiteSpace(childWhereSql))
                sql += " AND " + childWhereSql;

            var children = await _db.QueryAsync<TChild>(sql, new { Ids = ids }, cancellationToken: ct);
            var lookup = children
                .GroupBy(x => childForeignKeyProperty.GetValue(x))
                .ToDictionary(x => x.Key, x => (IReadOnlyList<TChild>)x.ToList());

            foreach (var parent in parents)
            {
                var key = parentKeyProperty.GetValue(parent);
                var rows = key is not null && lookup.TryGetValue(key, out var found) ? found : Array.Empty<TChild>();
                AssignChildren(parent, rows, target, backingField);
            }
        });

        return this;
    }

    /// <summary>
    /// Executes the Any operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the Any operation.</returns>
    public bool Any(string parentSql, object? parameters = null)
        => _db.ExecuteScalar<int>($"SELECT CASE WHEN EXISTS ({parentSql}) THEN 1 ELSE 0 END", parameters) > 0;

    /// <summary>
    /// Executes the AnyAsync operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the AnyAsync operation.</returns>
    public async Task<bool> AnyAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default)
        => await _db.ExecuteScalarAsync<int>($"SELECT CASE WHEN EXISTS ({parentSql}) THEN 1 ELSE 0 END", parameters, cancellationToken: cancellationToken) > 0;

    /// <summary>
    /// Executes the FirstOrDefault operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the FirstOrDefault operation.</returns>
    public TParent? FirstOrDefault(string parentSql, object? parameters = null)
        => FirstOrDefaultAsync(parentSql, parameters).GetAwaiter().GetResult();

    /// <summary>
    /// Executes the FirstOrDefaultAsync operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the FirstOrDefaultAsync operation.</returns>
    public async Task<TParent?> FirstOrDefaultAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default)
        => (await ToListAsync(parentSql, parameters, cancellationToken)).FirstOrDefault();

    /// <summary>
    /// Executes the ToList operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the ToList operation.</returns>
    public IReadOnlyList<TParent> ToList(string parentSql, object? parameters = null)
        => ToListAsync(parentSql, parameters).GetAwaiter().GetResult();

    /// <summary>
    /// Executes the ToListAsync operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ToListAsync operation.</returns>
    public async Task<IReadOnlyList<TParent>> ToListAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        var parents = (await _db.QueryAsync<TParent>(parentSql, parameters, cancellationToken: cancellationToken)).ToList();
        foreach (var include in _includes)
            await include(parents, cancellationToken);
        return parents;
    }

    private static PropertyInfo FindProperty(Type type, string name)
        => type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
           ?? throw new InvalidOperationException($"Property '{name}' was not found on {type.Name}.");

    private static void AssignChildren<TChild>(
        TParent parent,
        IReadOnlyList<TChild> children,
        Expression<Func<TParent, IEnumerable<TChild>>>? target,
        string? backingField)
    {
        if (!string.IsNullOrWhiteSpace(backingField))
        {
            var field = typeof(TParent).GetField(backingField, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new InvalidOperationException($"Backing field '{backingField}' was not found on {typeof(TParent).Name}.");

            if (field.GetValue(parent) is IList<TChild> list)
            {
                list.Clear();
                foreach (var child in children) list.Add(child);
                return;
            }

            field.SetValue(parent, children.ToList());
            return;
        }

        if (target is null)
            return;

        var member = target.Body is MemberExpression m ? m : null;
        if (member?.Member is not PropertyInfo property)
            throw new InvalidOperationException("Target must be a collection property expression, for example x => x.Items.");

        if (property.CanWrite)
        {
            property.SetValue(parent, ConvertChildren(children, property.PropertyType));
            return;
        }

        if (property.GetValue(parent) is IList<TChild> existing)
        {
            existing.Clear();
            foreach (var child in children) existing.Add(child);
        }
    }

    private static object ConvertChildren<TChild>(IReadOnlyList<TChild> children, Type targetType)
    {
        if (targetType.IsAssignableFrom(children.GetType())) return children;
        if (targetType.IsAssignableFrom(typeof(List<TChild>))) return children.ToList();
        if (targetType.IsArray) return children.ToArray();
        return children.ToList();
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
    /// Executes the Begin operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <returns>The result of the Begin operation.</returns>
    public static ForgeTransaction Begin(DbConnection connection) => new(connection, connection.BeginTransaction());
    /// <summary>
    /// Executes the BeginAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="ct">The ct value.</param>
    /// <returns>The result of the BeginAsync operation.</returns>
    public static async Task<ForgeTransaction> BeginAsync(DbConnection connection, CancellationToken ct) => new(connection, await connection.BeginTransactionAsync(ct));

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public IEnumerable<T> Query<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
        => ForgeAdo.Query<T>(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds).ToList();

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken  = default)
        => ForgeAdo.QueryAsync<T>(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);

    /// <summary>
    /// Executes the Execute operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the Execute operation.</returns>
    public int Execute(string sql, object? parameters = null, int? timeoutSeconds = null)
        => ForgeAdo.Execute(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds);

    /// <summary>
    /// Executes the ExecuteAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ExecuteAsync operation.</returns>
    public Task<int> ExecuteAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgeAdo.ExecuteAsync(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public T? ExecuteScalar<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
        => ForgeAdo.ExecuteScalar<T>(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgeAdo.ExecuteScalarAsync<T>(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);
    /// <summary>
    /// Executes the Commit operation.
    /// </summary>
    public void Commit() => _transaction.Commit();
    /// <summary>
    /// Executes the CommitAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the CommitAsync operation.</returns>
    public Task CommitAsync(CancellationToken cancellationToken = default) => _transaction.CommitAsync(cancellationToken);
    /// <summary>
    /// Executes the Rollback operation.
    /// </summary>
    public void Rollback() => _transaction.Rollback();
    /// <summary>
    /// Executes the RollbackAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the RollbackAsync operation.</returns>
    public Task RollbackAsync(CancellationToken cancellationToken = default) => _transaction.RollbackAsync(cancellationToken);
    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    /// <param name="_connection">The _connection value.</param>
    public void Dispose() { _transaction.Dispose(); _connection.Dispose(); }
    /// <summary>
    /// Executes the DisposeAsync operation.
    /// </summary>
    /// <param name="_connection">The _connection value.</param>
    /// <returns>The result of the DisposeAsync operation.</returns>
    public async ValueTask DisposeAsync() { await _transaction.DisposeAsync(); await _connection.DisposeAsync(); }
}
