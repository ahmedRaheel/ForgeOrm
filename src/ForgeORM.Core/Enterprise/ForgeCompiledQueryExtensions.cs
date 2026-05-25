using System.Linq.Expressions;

namespace ForgeORM.Core;

public static class ForgeCompiledQueryExtensions
{
    public static ForgeCompiledQuery<TEntity> CompiledQuery<TEntity>(this ForgeDb db, string name)
        where TEntity : class, new()
        => new(db, name);
}
