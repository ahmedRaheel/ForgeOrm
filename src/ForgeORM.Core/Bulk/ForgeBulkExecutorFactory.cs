using System;
using System.Data.Common;

namespace ForgeORM.Core;

internal static class ForgeBulkExecutorFactory
{
    public static IForgeProviderBulkExecutor Resolve(DbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var name = connection.GetType().FullName ?? string.Empty;

        if (name.Contains("SqlConnection", StringComparison.OrdinalIgnoreCase))
            return SqlServerBulkExecutor.Instance;

        if (name.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            return PostgreSqlBulkExecutor.Instance;

        if (name.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            return MySqlBulkExecutor.Instance;

        if (name.Contains("Oracle", StringComparison.OrdinalIgnoreCase))
            return OracleBulkExecutor.Instance;

        return GenericBulkExecutor.Instance;
    }
}
