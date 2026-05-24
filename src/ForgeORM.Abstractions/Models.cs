using System.Data;

namespace ForgeORM.Abstractions;


/// <summary>
/// Represents a single named parameter without allocating an anonymous object.
/// This is used by hot paths such as GetById/Find/Delete and by provider generated executors.
/// </summary>
public interface IForgeNamedParameter
{
    string Name { get; }
    object? BoxedValue { get; }
    Type ValueType { get; }
}

/// <summary>
/// Allocation-light strongly typed named parameter. Prefer ForgeParameters.Id(id) over new { Id = id } in hot paths.
/// </summary>
public readonly record struct ForgeNamedParameter<T>(string Name, T Value) : IForgeNamedParameter
{
    object? IForgeNamedParameter.BoxedValue => Value;
    Type IForgeNamedParameter.ValueType => typeof(T);
}

/// <summary>Factory helpers for strongly typed query parameters.</summary>
public static class ForgeParameters
{
    public static ForgeNamedParameter<T> Id<T>(T value) => new("Id", value);
    public static ForgeNamedParameter<T> Of<T>(string name, T value) => new(name, value);
}

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

public sealed class ForgeProviderCapabilities
{
    public bool SupportsBulkInsert { get; init; }
    public bool SupportsBulkUpdate { get; init; }
    public bool SupportsBulkDelete { get; init; }
    public bool SupportsBulkMerge { get; init; }
    public bool SupportsStoredProcedures { get; init; }
    public bool SupportsFunctions { get; init; }
    public bool SupportsTableValuedParameters { get; init; }
    public bool SupportsArrayParameters { get; init; }
    public bool SupportsCopy { get; init; }
    public bool SupportsReturningClause { get; init; }
    public bool SupportsJsonColumns { get; init; }
    public bool SupportsRefCursor { get; init; }
}

public sealed class ForgeEntityMetadata
{
    public required Type EntityType { get; init; }
    public required string TableName { get; init; }
    public required string KeyColumn { get; init; }
    public string CodeColumn { get; init; } = "Code";
    public IReadOnlyList<ForgePropertyMetadata> Properties { get; init; } = [];
}

public sealed class ForgePropertyMetadata
{
    public required string PropertyName { get; init; }
    public required string ColumnName { get; init; }
    public required Type PropertyType { get; init; }
    public bool IsKey { get; init; }
    public bool IsCode { get; init; }
    public bool IsComputed { get; init; }
}

public sealed class ForgePageRequest
{
    public required string Sql { get; init; }
    public object? Parameters { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public required string OrderBy { get; init; }
    public int Skip => Math.Max(Page - 1, 0) * PageSize;
}

public sealed class ForgePagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalRecords { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalRecords / (double)PageSize);
}

public sealed class ForgeQueryAnalysis
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
    public List<string> Suggestions { get; init; } = [];
}

public sealed class ForgeBuiltQuery
{
    public required string Sql { get; init; }
    public object? Parameters { get; init; }
}
