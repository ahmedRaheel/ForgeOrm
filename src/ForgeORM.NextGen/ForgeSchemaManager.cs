using ForgeORM.Abstractions;

namespace ForgeORM.NextGen;

public sealed class ForgeSchemaManager : IForgeSchemaManager
{
    private readonly IForgeDb _db;

    /// <summary>
    /// Executes the ForgeSchemaManager operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    /// <returns>The result of the ForgeSchemaManager operation.</returns>
    public ForgeSchemaManager(IForgeDb db)
    {
        _db = db;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    public ForgeSchemaDiff GenerateDiff<T>()
    {
        var table = typeof(T).Name;
        return new ForgeSchemaDiff
        {
            Changes = [$"Schema diff placeholder generated for {table}. Provider-specific implementation should compare model metadata with INFORMATION_SCHEMA."],
            FixScript = $"-- TODO: generated ALTER scripts for {table}"
        };
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask<ForgeSchemaDiff> GenerateDiffAsync<T>(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(GenerateDiff<T>());
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    public ForgeSchemaVerificationResult VerifySchema<T>()
    {
        var diff = GenerateDiff<T>();
        return new ForgeSchemaVerificationResult
        {
            Errors = diff.HasChanges ? diff.Changes : [],
            FixScript = diff.FixScript
        };
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask<ForgeSchemaVerificationResult> VerifySchemaAsync<T>(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(VerifySchema<T>());
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    public string SyncSchema<T>()
    {
        return GenerateDiff<T>().FixScript;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask<string> SyncSchemaAsync<T>(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(SyncSchema<T>());
    }
}
