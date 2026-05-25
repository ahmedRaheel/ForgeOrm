using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

/// <summary>
/// Optional provider-specific materializer hook. Provider packages can register typed readers
/// such as SqlDataReader/NpgsqlDataReader without forcing ForgeORM.Core to reference provider assemblies.
/// </summary>
public interface IForgeProviderMaterializer
{
    bool TryCreateReader<T>(DbDataReader reader, out Func<DbDataReader, T>? materializer);
}
