using System.Linq.Expressions;
using ForgeORM.Core.Materialization;

namespace ForgeORM.Core.EntryStyles;

/// <summary>
/// Raw SQL entry-point extensions.
/// </summary>
public static class ForgeRawSqlEntryExtensions
{
    /// <summary>
    /// Starts a raw SQL query with terminal materializers.
    /// </summary>
    public static ForgeRawSqlQuery Sql(
        this ForgeDb db,
        string sql,
        object? parameters = null)
    {
        return new ForgeRawSqlQuery(db, sql, parameters);
    }

    /// <summary>
    /// Alias for developers who prefer Raw().
    /// </summary>
    public static ForgeRawSqlQuery Raw(
        this ForgeDb db,
        string sql,
        object? parameters = null)
    {
        return new ForgeRawSqlQuery(db, sql, parameters);
    }
}
