using System.Linq.Expressions;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    /// <summary>
    /// Inserts a parent entity and its child collection in a single transaction using the mapped parent key automatically.
    /// </summary>
    /// <typeparam name="TParent">The parent entity type.</typeparam>
    /// <typeparam name="TChild">The child entity type.</typeparam>
    /// <param name="parent">The parent entity to insert.</param>
    /// <param name="children">The child collection expression on the parent entity.</param>
    /// <param name="childForeignKey">The child foreign-key expression that should receive the inserted parent key.</param>
    /// <param name="cancellationToken">The cancellation token for the async operation.</param>
    /// <returns>The inserted parent key value.</returns>
    public async ValueTask<object?> InsertGraphAsync<TParent, TChild>(
        TParent parent,
        Expression<Func<TParent, IEnumerable<TChild>>> children,
        Expression<Func<TChild, object>> childForeignKey,
        CancellationToken cancellationToken = default)
    {
        if (parent is null) throw new ArgumentNullException(nameof(parent));
        if (children is null) throw new ArgumentNullException(nameof(children));
        if (childForeignKey is null) throw new ArgumentNullException(nameof(childForeignKey));

        var parentShape = ForgeEntityShape.For(typeof(TParent));
        var parentKey = parentShape.KeyProperty
            ?? throw new InvalidOperationException($"ForgeORM graph insert requires a key property on {typeof(TParent).Name}.");
        var childForeignKeyProperty = ForgeExpression.Property(childForeignKey.Body);
        var childRows = ForgeExpressionDelegateCache.Get(children)(parent)?.ToList() ?? [];

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var options = new ForgeGraphInsertOptions<TParent, TParent>();
            options.ParentOptions.KeyProperty = parentKey;
            var key = await InsertParentAndReturnKeyAsync<TParent, TParent, object>(
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
    /// Inserts a DTO graph by mapping the DTO to a parent entity and mapping its child DTO collection to child entities.
    /// </summary>
    /// <typeparam name="TDto">The source DTO type.</typeparam>
    /// <typeparam name="TParent">The parent entity type.</typeparam>
    /// <typeparam name="TChildDto">The source child DTO type.</typeparam>
    /// <typeparam name="TChild">The child entity type.</typeparam>
    /// <param name="dto">The DTO containing parent and child data.</param>
    /// <param name="children">The child DTO collection expression.</param>
    /// <param name="childForeignKey">The child entity foreign-key expression that should receive the inserted parent key.</param>
    /// <param name="cancellationToken">The cancellation token for the async operation.</param>
    /// <returns>The inserted parent key value.</returns>
    public ValueTask<object?> InsertGraphAsync<TDto, TParent, TChildDto, TChild>(
        TDto dto,
        Expression<Func<TDto, IEnumerable<TChildDto>>> children,
        Expression<Func<TChild, object>> childForeignKey,
        CancellationToken cancellationToken = default)
        where TParent : new()
        where TChild : new()
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        if (children is null) throw new ArgumentNullException(nameof(children));

        var parent = ForgeObjectMapper.Map<TParent>(dto!);
        var childRows = ForgeExpressionDelegateCache.Get(children)(dto)?.Select(child => ForgeObjectMapper.Map<TChild>(child!)).ToList() ?? [];
        return InsertGraphAsync(parent, _ => childRows, childForeignKey, cancellationToken);
    }
}
