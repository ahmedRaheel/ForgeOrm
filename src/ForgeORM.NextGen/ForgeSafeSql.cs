using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using ForgeORM.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace ForgeORM.NextGen;

public sealed class ForgeSafeSql
{
    public required string Sql { get; init; }
    public required Dictionary<string, object?> Parameters { get; init; }
}
