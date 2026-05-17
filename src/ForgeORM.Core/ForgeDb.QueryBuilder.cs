namespace ForgeORM.Core;

public partial class ForgeDb
{
    /// <summary>
    /// Starts a typed expression query builder.
    /// </summary>
    public ForgeQueryBuilder<TEntity> Query<TEntity>()
        where TEntity : class, new()
    {
        return new ForgeQueryBuilder<TEntity>(this);
    }
}
