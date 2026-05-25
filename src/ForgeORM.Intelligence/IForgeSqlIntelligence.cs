namespace ForgeORM.Intelligence;

public interface IForgeSqlIntelligence
/// <summary>
/// Defines the Suggest operation.
/// </summary>
/// <param name="partialSql">The partialSql value.</param>
/// <param name="context">The context value.</param>
/// <returns>The result of the Suggest operation.</returns>
{
    /// <summary>
    /// Defines the Suggest operation.
    /// </summary>
    /// <param name="partialSql">The partialSql value.</param>
    /// <param name="context">The context value.</param>
    /// <returns>The result of the Suggest operation.</returns>
    ForgeSqlSuggestionResult Suggest(string partialSql, ForgeSqlContext context);
    /// <summary>
    /// Defines the Correct operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="context">The context value.</param>
    /// <returns>The result of the Correct operation.</returns>
    ForgeSqlCorrectionResult Correct(string sql, ForgeSqlContext context);
    /// <summary>
    /// Defines the Complete operation.
    /// </summary>
    /// <param name="partialSql">The partialSql value.</param>
    /// <param name="cursorPosition">The cursorPosition value.</param>
    /// <param name="context">The context value.</param>
    /// <returns>The result of the Complete operation.</returns>
    ForgeSqlCompletionResult Complete(string partialSql, int cursorPosition, ForgeSqlContext context);
}
