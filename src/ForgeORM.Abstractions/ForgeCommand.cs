using System.Data;

namespace ForgeORM.Abstractions;

public sealed class ForgeCommand
{
    /// <summary>
    /// Executes the Text operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the Text operation.</returns>
    public required string CommandText { get; init; }
    /// <summary>
    /// Executes the Text operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the Text operation.</returns>
    public object? Parameters { get; init; }
    /// <summary>
    /// Executes the Text operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the Text operation.</returns>
    public CommandType CommandType { get; init; } = CommandType.Text;
    /// <summary>
    /// Executes the Text operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the Text operation.</returns>
    public int? TimeoutSeconds { get; init; }

    /// <summary>
    /// Executes the Text operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the Text operation.</returns>
    public static ForgeCommand Text(string sql, object? parameters = null, int? timeoutSeconds = null)
        => new() { CommandText = sql, Parameters = parameters, TimeoutSeconds = timeoutSeconds, CommandType = CommandType.Text };

    /// <summary>
    /// Executes the StoredProcedure operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the StoredProcedure operation.</returns>
    public static ForgeCommand StoredProcedure(string name, object? parameters = null, int? timeoutSeconds = null)
        => new() { CommandText = name, Parameters = parameters, TimeoutSeconds = timeoutSeconds, CommandType = CommandType.StoredProcedure };
}
