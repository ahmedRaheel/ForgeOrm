using System.Data;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Providers.SqlServer;

/// <summary>
/// Routes SQL Server bulk operations through the primary SqlDataRecord TVP path first,
/// then falls back to DataTable TVP/table-type parameter only when the primary path
/// is unavailable. This class intentionally does not use ForgeSqlBulkCopyRemoved_DoNotUse.
/// </summary>
internal static class SqlServerBulkRoutingPolicy
{
    public static bool CanFallback(Exception exception)
    {
        return exception is TypeLoadException
            or MissingMethodException
            or InvalidCastException
            or NotSupportedException
            or FileNotFoundException;
    }
}
