using System.Data;
using System.Text;

namespace ForgeORM.Core.Providers;

public static class ForgeProviderDialectRegistry
{
    public static IForgeProviderDialect Create(ForgePhysicalProvider provider)
        => provider switch
        {
            ForgePhysicalProvider.SqlServer => new SqlServerForgeDialect(),
            ForgePhysicalProvider.PostgreSql => new PostgreSqlForgeDialect(),
            ForgePhysicalProvider.MySql => new MySqlForgeDialect(),
            ForgePhysicalProvider.Oracle => new OracleForgeDialect(),
            _ => new SqlServerForgeDialect()
        };
}
