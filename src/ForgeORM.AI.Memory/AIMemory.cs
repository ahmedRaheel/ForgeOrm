using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.AI.Memory;

public sealed record ForgeMemoryEntry(string Scope, string Key, string Value, DateTimeOffset CreatedUtc, IReadOnlyDictionary<string,string>? Tags = null);

public interface IForgeAiMemoryStore
{
    Task RememberAsync(ForgeMemoryEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ForgeMemoryEntry>> RecallAsync(string scope, string? query = null, CancellationToken cancellationToken = default);
}

public sealed class InMemoryForgeAiMemoryStore : IForgeAiMemoryStore
{
    private readonly ConcurrentBag<ForgeMemoryEntry> _entries = [];
    public Task RememberAsync(ForgeMemoryEntry entry, CancellationToken cancellationToken = default) { _entries.Add(entry); return Task.CompletedTask; }
    public Task<IReadOnlyList<ForgeMemoryEntry>> RecallAsync(string scope, string? query = null, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ForgeMemoryEntry>>(_entries.Where(x => x.Scope == scope && (string.IsNullOrWhiteSpace(query) || x.Key.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Value.Contains(query, StringComparison.OrdinalIgnoreCase))).OrderByDescending(x => x.CreatedUtc).ToList());
}

public static class ForgeAiMemoryServiceCollectionExtensions
{
    public static IServiceCollection AddForgeAiMemory(this IServiceCollection services) => services.AddSingleton<IForgeAiMemoryStore, InMemoryForgeAiMemoryStore>();
}
