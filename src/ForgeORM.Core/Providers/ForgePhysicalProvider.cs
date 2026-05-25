using System.Data;
using System.Text;

namespace ForgeORM.Core.Providers;

/// <summary>
/// Supported physical database providers for production execution.
/// </summary>
public enum ForgePhysicalProvider
{
    SqlServer,
    PostgreSql,
    MySql,
    Oracle,
    Sqlite
}
