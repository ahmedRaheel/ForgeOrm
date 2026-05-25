using System.Data.Common;
using System.Runtime.CompilerServices;
using ForgeORM.Core;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Providers.SqlServer;

/// <summary>
/// Registers SQL Server typed-reader materializer hooks. ForgeORM.Core remains provider-neutral,
/// while SQL Server workloads can opt into reader-specific hot paths from the provider package.
/// </summary>
public sealed class SqlServerProviderTypedMaterializer : IForgeProviderMaterializer
{
    [ModuleInitializer]
    internal static void Register() => ForgeProviderMaterializerRegistry.Register(new SqlServerProviderTypedMaterializer());

    public bool TryCreateReader<T>(DbDataReader reader, out Func<DbDataReader, T>? materializer)
    {
        if (reader is not SqlDataReader)
        {
            materializer = null;
            return false;
        }

        // Current implementation reuses ForgeORM's shape-based MSIL reader but registers the provider hook
        // here so provider-specific source/MSIL readers can replace this implementation without changing Core.
        var compiled = ForgeMaterializer.GetReader<T>(reader);
        materializer = compiled;
        return true;
    }
}
