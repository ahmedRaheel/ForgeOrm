namespace ForgeORM.Core.SavedQueries;

/// <summary>
/// Saved query extension methods.
/// </summary>
public static class ForgeSavedQueryExtensions
{
    /// <summary>
    /// Creates a saved query manager for this database instance.
    /// </summary>
    public static ForgeSavedQueryManager SavedQueries(this ForgeDb db)
        => new(db);

    /// <summary>
    /// Registers or replaces a saved query for this database instance.
    /// </summary>
    public static ForgeSavedQueryManager RegisterSavedQuery(
        this ForgeDb db,
        string name,
        string sql,
        object? parameters = null,
        string? description = null)
    {
        return new ForgeSavedQueryManager(db)
            .Register(name, sql, parameters, description);
    }
}
