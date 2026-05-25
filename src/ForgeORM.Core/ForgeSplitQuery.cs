using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using ForgeORM.Core.Performance;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

internal sealed class ForgeSplitQuery<TParent> : IForgeSplitQuery<TParent>
{
    private readonly IForgeDb _db;
    private readonly List<Func<IReadOnlyList<TParent>, CancellationToken, ValueTask>> _includes = [];

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
                .Select(x => ForgeRuntimeAccessorCache.Get(parentKeyProperty, x!))
                .Where(x => x is not null)
                .Distinct()
                .ToList();

            if (ids.Count == 0) return;

            var sql = $"SELECT * FROM {childTable} WHERE {childForeignKey} IN @Ids";
            if (!string.IsNullOrWhiteSpace(childWhereSql))
                sql += " AND " + childWhereSql;

            var children = await _db.QueryAsync<TChild>(sql, new { Ids = ids }, cancellationToken: ct);
            var lookup = children
                .GroupBy(x => ForgeRuntimeAccessorCache.Get(childForeignKeyProperty, x!))
                .ToDictionary(x => x.Key, x => (IReadOnlyList<TChild>)x.ToList());

            foreach (var parent in parents)
            {
                var key = ForgeRuntimeAccessorCache.Get(parentKeyProperty, parent!);
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
    public async ValueTask<bool> AnyAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default)
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
    public async ValueTask<TParent?> FirstOrDefaultAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default)
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
    public async ValueTask<IReadOnlyList<TParent>> ToListAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default)
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

            if (ForgeRuntimeMemberCache.Get(field, parent!) is IList<TChild> list)
            {
                list.Clear();
                foreach (var child in children) list.Add(child);
                return;
            }

            ForgeRuntimeMemberCache.Set(field, parent!, children.ToList());
            return;
        }

        if (target is null)
            return;

        var member = target.Body is MemberExpression m ? m : null;
        if (member?.Member is not PropertyInfo property)
            throw new InvalidOperationException("Target must be a collection property expression, for example x => x.Items.");

        if (property.CanWrite)
        {
            ForgeRuntimeAccessorCache.Set(property, parent!, ConvertChildren(children, property.PropertyType));
            return;
        }

        if (ForgeRuntimeAccessorCache.Get(property, parent!) is IList<TChild> existing)
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
