using ForgeORM.Abstractions;
using ForgeORM.Core.Search;

namespace ForgeORM.Core;

/// <summary>
/// EF-style ForgeORM database context.
/// Consumers inject this class and call high-level ForgeORM methods directly,
/// without creating connections, commands, readers, mappers, or extension plumbing.
/// </summary>
public class ForgeDbContext : ForgeDb
{
    public ForgeDbContext(
        string connectionString,
        IForgeDatabaseProvider provider,
        IForgeEntityMetadataResolver metadata,
        IForgeQueryAnalyzer analyzer)
        : base(connectionString, provider, metadata, analyzer)
    {
    }

    /// <summary>
    /// EF-style alias: context.Entities&lt;Product&gt;().Where(...).ToListAsync().
    /// </summary>
    public IForgeQuery<T> Entities<T>() => Set<T>();

    /// <summary>
    /// EF-style alias for raw SQL query composition.
    /// </summary>
    public IForgeQuery<T> FromSql<T>(string sql, object? parameters = null) => Sql<T>(sql, parameters);

    /// <summary>ForgeORM search builder with expression-based optional filters and paging.</summary>
    public ForgeSearch<T> Search<T>() => new(this);

    /// <summary>ForgeORM stored procedure search builder.</summary>
    public ForgeProcedureSearch<T> SearchProcedure<T>(string procedureName) => new(this, procedureName);

}
