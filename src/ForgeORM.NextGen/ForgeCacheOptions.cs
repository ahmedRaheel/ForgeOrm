using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using ForgeORM.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace ForgeORM.NextGen;

public sealed class ForgeCacheOptions
{
    public TimeSpan Duration { get; init; }
    public string? Key { get; init; }
    public bool UseMemoryCache { get; init; } = true;
}
