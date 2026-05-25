namespace ForgeORM.Core.SplitQuery;

internal interface IForgeSplitInclude<TParent>
    where TParent : class
{
    ValueTask ApplyAsync(
        ForgeDbContext db,
        IReadOnlyList<TParent> parents,
        CancellationToken cancellationToken);
}
