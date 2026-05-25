using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Abstractions;
using ForgeORM.Core.Graph;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    /// <summary>
    /// Executes the TDto operation.
    /// </summary>
    /// <typeparam name="TEntity">The type used by the operation.</typeparam>
    /// <typeparam name="TDto">The type used by the operation.</typeparam>
    /// <param name="dto">The dto value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the TDto operation.</returns>
    public ValueTask<int> InsertAsync<TEntity, TDto>(TDto dto, CancellationToken cancellationToken = default)
        where TEntity : new()
    {
        var entity = ForgeObjectMapper.Map<TEntity>(dto!);
        return InsertAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Executes the TKey operation.
    /// </summary>
    /// <typeparam name="TParent">The type used by the operation.</typeparam>
    /// <typeparam name="TDto">The type used by the operation.</typeparam>
    /// <typeparam name="TKey">The type used by the operation.</typeparam>
    /// <param name="dto">The dto value.</param>
    /// <param name="configure">The configure value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the TKey operation.</returns>
    public async ValueTask<TKey> InsertGraphAsync<TParent, TDto, TKey>(
        TDto dto,
        Action<ForgeGraphInsertOptions<TParent, TDto>> configure,
        CancellationToken cancellationToken = default)
        where TParent : new()
    {
        var options = new ForgeGraphInsertOptions<TParent, TDto>();
        configure(options);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var parent = ForgeObjectMapper.Map<TParent>(dto!);
            var parentKey = await InsertParentAndReturnKeyAsync<TParent, TDto, TKey>(connection, transaction, parent, options, cancellationToken);

            if (options.IncludeChildren)
            {
                foreach (var child in options.ChildMappings)
                {
                    await child.InsertAsync(connection, transaction, dto!, parentKey!, cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return parentKey;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Inserts the parent entity from a DTO using graph runtime options. This overload is useful
    /// for parent-only inserts or when samples want to demonstrate strategy selection without
    /// explicit child mapping. Use the graph-mapping overload for parent + children.
    /// </summary>
    public async ValueTask<TKey> InsertGraphAsync<TParent, TDto, TKey>(
        TDto dto,
        Action<ForgeGraphOptions> configure,
        CancellationToken cancellationToken = default)
        where TParent : new()
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ForgeGraphOptions();
        configure(options);

        var graphOptions = new ForgeGraphInsertOptions<TParent, TDto>
        {
            IncludeChildren = false,
            UseBulkWhenPossible = options.UseBulkWhenPossible,
            BatchSize = options.BatchSize,
            Strategy = options.Strategy
        };

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var parent = ForgeObjectMapper.Map<TParent>(dto!);
            var parentKey = await InsertParentAndReturnKeyAsync<TParent, TDto, TKey>(
                connection,
                transaction,
                parent,
                graphOptions,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return parentKey;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Executes the TDto operation.
    /// </summary>
    /// <typeparam name="TParent">The type used by the operation.</typeparam>
    /// <typeparam name="TDto">The type used by the operation.</typeparam>
    /// <param name="dto">The dto value.</param>
    /// <param name="configure">The configure value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the TDto operation.</returns>
    public async ValueTask<object?> InsertGraphAsync<TParent, TDto>(
        TDto dto,
        Action<ForgeGraphInsertOptions<TParent, TDto>> configure,
        CancellationToken cancellationToken = default)
        where TParent : new()
    {
        var key = await InsertGraphAsync<TParent, TDto, object>(dto, configure, cancellationToken);
        return key;
    }


    /// <summary>
    /// Executes the TKey operation.
    /// </summary>
    /// <typeparam name="TParent">The type used by the operation.</typeparam>
    /// <typeparam name="TChild">The type used by the operation.</typeparam>
    /// <typeparam name="TKey">The type used by the operation.</typeparam>
    /// <param name="parent">The parent value.</param>
    /// <param name="children">The children value.</param>
    /// <param name="parentKey">The parentKey value.</param>
    /// <param name="childForeignKey">The childForeignKey value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the TKey operation.</returns>
    public async ValueTask<TKey> InsertGraphAsync<TParent, TChild, TKey>(
        TParent parent,
        Expression<Func<TParent, IEnumerable<TChild>>> children,
        Expression<Func<TParent, TKey>> parentKey,
        Expression<Func<TChild, TKey>> childForeignKey,
        CancellationToken cancellationToken = default)
    {
        if (parent is null) throw new ArgumentNullException(nameof(parent));

        var parentKeyProperty = ForgeExpression.Property(parentKey.Body);
        var childForeignKeyProperty = ForgeExpression.Property(childForeignKey.Body);
        var childAccessor = ForgeExpressionDelegateCache.Get(children);
        var childRows = childAccessor(parent)?.ToList() ?? [];

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var options = new ForgeGraphInsertOptions<TParent, TParent>();
            options.ParentOptions.KeyProperty = parentKeyProperty;
            var key = await InsertParentAndReturnKeyAsync<TParent, TParent, TKey>(
                connection,
                transaction,
                parent,
                options,
                cancellationToken);

            foreach (var child in childRows)
            {
                if (child is null) continue;
                if (childForeignKeyProperty.CanWrite)
                    ForgeRuntimeAccessorCache.Set(childForeignKeyProperty, child!, ForgeObjectMapper.ConvertTo(key, childForeignKeyProperty.PropertyType));

                ForgeEntityShape.EnsureGeneratedKey(child);
                ResetDatabaseGeneratedIdentity(child);
            }

            await InsertChildrenRowByRowAsync(connection, transaction, childRows, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return key;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Executes the TKey operation.
    /// </summary>
    /// <typeparam name="TDto">The type used by the operation.</typeparam>
    /// <typeparam name="TParent">The type used by the operation.</typeparam>
    /// <typeparam name="TChildDto">The type used by the operation.</typeparam>
    /// <typeparam name="TChild">The type used by the operation.</typeparam>
    /// <typeparam name="TKey">The type used by the operation.</typeparam>
    /// <param name="dto">The dto value.</param>
    /// <param name="parentFactory">The parentFactory value.</param>
    /// <param name="children">The children value.</param>
    /// <param name="childFactory">The childFactory value.</param>
    /// <param name="parentKey">The parentKey value.</param>
    /// <param name="childForeignKey">The childForeignKey value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the TKey operation.</returns>
    public ValueTask<TKey> InsertGraphAsync<TDto, TParent, TChildDto, TChild, TKey>(
        TDto dto,
        Func<TDto, TParent> parentFactory,
        Func<TDto, IEnumerable<TChildDto>> children,
        Func<TParent, TChildDto, TChild> childFactory,
        Expression<Func<TParent, TKey>> parentKey,
        Expression<Func<TChild, TKey>> childForeignKey,
        CancellationToken cancellationToken = default)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        if (parentFactory is null) throw new ArgumentNullException(nameof(parentFactory));
        if (children is null) throw new ArgumentNullException(nameof(children));
        if (childFactory is null) throw new ArgumentNullException(nameof(childFactory));

        var parent = parentFactory(dto);
        var childRows = children(dto).Select(child => childFactory(parent, child)).ToList();
        return InsertGraphAsync(
            parent,
            _ => childRows,
            parentKey,
            childForeignKey,
            cancellationToken);
    }

    private static async ValueTask InsertChildrenRowByRowAsync<TChild>(
        DbConnection connection,
        DbTransaction transaction,
        IReadOnlyList<TChild> children,
        CancellationToken cancellationToken)
    {
        if (children.Count == 0) return;

        var shape = ForgeEntityShape.For(typeof(TChild));
        var key = shape.KeyProperty;
        var props = ForgeGraphWriteHelpers.GetInsertProperties(shape, includeKey: false);
        var sql = ForgeGraphWriteHelpers.BuildInsertSql(shape, props, includeScopeIdentity: key is not null);

        foreach (var child in children)
        {
            if (child is null) continue;
            ResetDatabaseGeneratedIdentity(child!);
            var parameters = ForgeGraphWriteHelpers.CreateParameterDictionary(props, child!);
            await using var command = ForgeAdo.CreateCommand(connection, sql, parameters, transaction);
            if (key is not null)
            {
                var generated = await command.ExecuteScalarAsync(cancellationToken);
                if (key.CanWrite)
                    ForgeRuntimeAccessorCache.Set(key, child!, ForgeObjectMapper.ConvertTo(generated, key.PropertyType));
            }
            else
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }

    private static void ResetDatabaseGeneratedIdentity(object entity)
    {
        var shape = ForgeEntityShape.For(entity.GetType());
        var key = shape.KeyProperty;
        if (key is null || !key.CanWrite)
            return;

        var keyType = Nullable.GetUnderlyingType(key.PropertyType) ?? key.PropertyType;
        if (keyType == typeof(Guid))
            return;

        if (keyType == typeof(int) || keyType == typeof(long) || keyType == typeof(short))
            ForgeRuntimeAccessorCache.Set(key, entity, ForgeRuntimeAccessorCache.DefaultValue(keyType));
    }

    private static async ValueTask<TKey> InsertParentAndReturnKeyAsync<TParent, TDto, TKey>(
        DbConnection connection,
        DbTransaction transaction,
        TParent parent,
        ForgeGraphInsertOptions<TParent, TDto> options,
        CancellationToken cancellationToken)
    {
        var entity = ForgeEntityShape.For(typeof(TParent));
        var key = options.ParentOptions.KeyProperty ?? entity.KeyProperty;
        if (key is null)
            throw new InvalidOperationException($"ForgeORM graph insert requires a key property on {typeof(TParent).Name}. Use graph.Parent().Key(x => x.Id).");

        EnsureKeyValue(parent!, key);
        ResetDatabaseGeneratedIdentity(parent!);

        var keyValue = ForgeRuntimeAccessorCache.Get(key, parent!);
        var includeKeyInInsert = ShouldIncludeKeyInInsert(key, keyValue);
        var props = ForgeGraphWriteHelpers.GetInsertProperties(entity, includeKeyInInsert);
        var sql = ForgeGraphWriteHelpers.BuildInsertSql(entity, props, includeScopeIdentity: !includeKeyInInsert);
        var parameters = ForgeGraphWriteHelpers.CreateParameterDictionary(props, parent!);
        await using var command = ForgeAdo.CreateCommand(connection, sql, parameters, transaction);
        var result = includeKeyInInsert ? keyValue : await command.ExecuteScalarAsync(cancellationToken);
        if (includeKeyInInsert)
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        else if (key.CanWrite)
        {
            ForgeRuntimeAccessorCache.Set(key, parent!, ForgeObjectMapper.ConvertTo(result, key.PropertyType));
        }

        return (TKey)ForgeObjectMapper.ConvertTo(result, typeof(TKey))!;
    }

    private static bool ShouldIncludeKeyInInsert(PropertyInfo key, object? value)
    {
        var type = Nullable.GetUnderlyingType(key.PropertyType) ?? key.PropertyType;
        if (type == typeof(Guid)) return true;
        if (value is null) return false;
        if (Equals(value, ForgeRuntimeAccessorCache.DefaultValue(type))) return false;
        return type != typeof(int) && type != typeof(long) && type != typeof(short);
    }

    private static void EnsureKeyValue(object entity, PropertyInfo key)
    {
        var type = Nullable.GetUnderlyingType(key.PropertyType) ?? key.PropertyType;
        var current = ForgeRuntimeAccessorCache.Get(key, entity);
        if (type == typeof(Guid) && key.CanWrite && (current is null || (Guid)current == Guid.Empty))
            ForgeRuntimeAccessorCache.Set(key, entity, Guid.NewGuid());
    }

    private static bool SameProperty(PropertyInfo a, PropertyInfo b)
        => a.Name.Equals(b.Name, StringComparison.OrdinalIgnoreCase);
}
