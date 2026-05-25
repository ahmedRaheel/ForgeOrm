using System.Data.Common;

namespace ForgeORM.Core;

public static class ForgeProviderExecutionStrategySelector
{
    public static IForgeProviderExecutionStrategy Resolve(DbConnection connection)
    {
        var name = connection.GetType().FullName ?? connection.GetType().Name;
        if (name.Contains("SqlClient", StringComparison.OrdinalIgnoreCase))
            return ForgeSqlServerExecutionStrategy.Instance;
        if (name.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) || name.Contains("Postgre", StringComparison.OrdinalIgnoreCase))
            return ForgePostgreSqlExecutionStrategy.Instance;
        if (name.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            return ForgeMySqlExecutionStrategy.Instance;
        if (name.Contains("Oracle", StringComparison.OrdinalIgnoreCase))
            return ForgeOracleExecutionStrategy.Instance;
        return ForgeGenericExecutionStrategy.Instance;
    }
}
