using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.Core;

/// <summary>
/// Entry-point helper for lambda-based saved query registration.
/// </summary>
public sealed class ForgeSavedQueryBuilderRoot
{
    private readonly ForgeDb _db;

    internal ForgeSavedQueryBuilderRoot(ForgeDb db)
    {
        _db = db;
    }

    internal object? CurrentBuilder { get; private set; }

    /// <summary>
    /// Starts a typed query from the mapped table for <typeparamref name="TEntity"/>.
    /// </summary>
    public ForgeQueryBuilder<TEntity> From<TEntity>()
        where TEntity : class, new()
    {
        var builder = new ForgeQueryBuilder<TEntity>(_db).From<TEntity>();
        CurrentBuilder = builder;
        return builder;
    }

    internal ForgeRenderedQuery Render()
    {
        if (CurrentBuilder is null)
        {
            throw new InvalidOperationException("Saved query registration must call query.From<TEntity>() before rendering.");
        }

        var render = CurrentBuilder.GetType().GetMethod(nameof(ForgeQueryBuilder<object>.Render));
        if (render is null)
        {
            throw new InvalidOperationException("Saved query builder does not expose Render().");
        }

        return (ForgeRenderedQuery)render.Invoke(CurrentBuilder, null)!;
    }
}
