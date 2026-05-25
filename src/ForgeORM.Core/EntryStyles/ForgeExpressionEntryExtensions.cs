using System.Linq.Expressions;
using ForgeORM.Core.Materialization;

namespace ForgeORM.Core.EntryStyles;

/// <summary>
/// Expression query convenience helpers that expose the same terminal mindset.
/// This intentionally sits beside the existing Query<T>() builder.
/// </summary>
public static class ForgeExpressionEntryExtensions
{
    public static ForgeExpressionQuery<TEntity> Expression<TEntity>(
        this ForgeDb db)
        where TEntity : class, new()
    {
        return new ForgeExpressionQuery<TEntity>(db);
    }
}
