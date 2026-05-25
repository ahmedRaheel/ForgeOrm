using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ForgeORM.Core;
using ForgeORM.QueryAst;

namespace ForgeORM.Core.Search;

/// <summary>
/// Entry-point extensions for enterprise search.
/// </summary>
public static class ForgeSearchExtensions
{
    /// <summary>
    /// Starts a dynamic search query.
    /// </summary>
    public static ForgeSearch<T> Search<T>(this ForgeDb db)
    {
        return new ForgeSearch<T>(db);
    }

    /// <summary>
    /// Starts a stored-procedure-backed search query.
    /// </summary>
    public static ForgeProcedureSearch<T> SearchProcedure<T>(
        this ForgeDb db,
        string procedureName)
    {
        return new ForgeProcedureSearch<T>(db, procedureName);
    }
}
