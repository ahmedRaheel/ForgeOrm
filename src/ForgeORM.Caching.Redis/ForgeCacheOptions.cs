using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Caching.Redis;

public sealed record ForgeCacheOptions(string KeyPrefix = "forgeorm", TimeSpan DefaultTtl = default)
{
    public TimeSpan EffectiveDefaultTtl => DefaultTtl == default ? TimeSpan.FromMinutes(10) : DefaultTtl;
}
