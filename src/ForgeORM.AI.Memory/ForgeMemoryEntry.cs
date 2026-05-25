using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.AI.Memory;

public sealed record ForgeMemoryEntry(string Scope, string Key, string Value, DateTimeOffset CreatedUtc, IReadOnlyDictionary<string,string>? Tags = null);
