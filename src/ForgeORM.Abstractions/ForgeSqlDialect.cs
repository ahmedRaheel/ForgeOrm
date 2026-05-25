using System.Data;

namespace ForgeORM.Abstractions;

public sealed class ForgeSqlDialect
{
    /// <summary>
    /// Executes the Parameter operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the Parameter operation.</returns>
    public required string Name { get; init; }
    /// <summary>
    /// Executes the Parameter operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the Parameter operation.</returns>
    public required string ParameterPrefix { get; init; }
    /// <summary>
    /// Executes the Parameter operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the Parameter operation.</returns>
    public required string OpenIdentifier { get; init; }
    /// <summary>
    /// Executes the Parameter operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the Parameter operation.</returns>
    public required string CloseIdentifier { get; init; }
    /// <summary>
    /// Executes the Parameter operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the Parameter operation.</returns>
    public string Parameter(string name) => $"{ParameterPrefix}{name}";
}
