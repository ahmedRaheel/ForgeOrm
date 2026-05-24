namespace ForgeORM.Security;

public interface IForgeSqlSecurityValidator
/// <summary>
/// Defines the Validate operation.
/// </summary>
/// <param name="sql">The sql value.</param>
/// <param name="allowDdl">The allowDdl value.</param>
/// <param name="allowDangerous">The allowDangerous value.</param>
/// <returns>The result of the Validate operation.</returns>
{
    /// <summary>
    /// Defines the Validate operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="allowDdl">The allowDdl value.</param>
    /// <param name="allowDangerous">The allowDangerous value.</param>
    /// <returns>The result of the Validate operation.</returns>
    ForgeSqlSafetyResult Validate(string sql, bool allowDdl = false, bool allowDangerous = false);
}
