using ForgeORM.Abstractions;
using ForgeORM.QueryAst;

namespace ForgeORM.QueryAst.Artifacts;

public static class ForgeAstArtifactExtensions
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="query">The query value.</param>
    /// <param name="name">The name value.</param>
    /// <param name="schema">The schema value.</param>
    /// <returns>The result of the T operation.</returns>
    public static ForgeViewArtifactBuilder<T> AsView<T>(this IForgeAstSelectBuilder<T> query, string name, string schema = "dbo")
        => new(query, name, schema);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="query">The query value.</param>
    /// <param name="name">The name value.</param>
    /// <param name="schema">The schema value.</param>
    /// <returns>The result of the T operation.</returns>
    public static ForgeProcedureArtifactBuilder<T> AsProcedure<T>(this IForgeAstSelectBuilder<T> query, string name, string schema = "dbo")
        => new(query, name, schema);
}
