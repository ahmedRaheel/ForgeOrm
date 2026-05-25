using ForgeORM.Abstractions;

namespace ForgeORM.Relationships;

public static class ForgeRelationshipSplitQueryExtensions
{
    /// <summary>
    /// Executes the TParent operation.
    /// </summary>
    /// <typeparam name="TParent">The type used by the operation.</typeparam>
    /// <param name="db">The db value.</param>
    /// <returns>The result of the TParent operation.</returns>
    public static ForgeRelationshipSplitQuery<TParent> SplitGraph<TParent>(this IForgeDb db) => new(db);
}
