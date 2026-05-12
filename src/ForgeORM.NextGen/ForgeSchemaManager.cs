using ForgeORM.Abstractions;

namespace ForgeORM.NextGen;

public sealed class ForgeSchemaManager : IForgeSchemaManager
{
    private readonly IForgeDb _db;

    public ForgeSchemaManager(IForgeDb db)
    {
        _db = db;
    }

    public ForgeSchemaDiff GenerateDiff<T>()
    {
        var table = typeof(T).Name;
        return new ForgeSchemaDiff
        {
            Changes = [$"Schema diff placeholder generated for {table}. Provider-specific implementation should compare model metadata with INFORMATION_SCHEMA."],
            FixScript = $"-- TODO: generated ALTER scripts for {table}"
        };
    }

    public Task<ForgeSchemaDiff> GenerateDiffAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GenerateDiff<T>());
    }

    public ForgeSchemaVerificationResult VerifySchema<T>()
    {
        var diff = GenerateDiff<T>();
        return new ForgeSchemaVerificationResult
        {
            Errors = diff.HasChanges ? diff.Changes : [],
            FixScript = diff.FixScript
        };
    }

    public Task<ForgeSchemaVerificationResult> VerifySchemaAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(VerifySchema<T>());
    }

    public string SyncSchema<T>()
    {
        return GenerateDiff<T>().FixScript;
    }

    public Task<string> SyncSchemaAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SyncSchema<T>());
    }
}
