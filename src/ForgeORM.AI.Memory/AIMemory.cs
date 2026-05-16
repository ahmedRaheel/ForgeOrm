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
    /// <summary>
    /// Initializes or executes the RememberAsync operation.
    /// </summary>
    /// <param name="entry">The entry value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public Task RememberAsync(ForgeMemoryEntry entry, CancellationToken cancellationToken = default) { _entries.Add(entry); return Task.CompletedTask; }
    /// <summary>
    /// Initializes or executes the RecallAsync operation.
    /// </summary>
    /// <param name="scope">The scope value.</param>
    /// <param name="query">The query value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public Task<IReadOnlyList<ForgeMemoryEntry>> RecallAsync(string scope, string? query = null, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ForgeMemoryEntry>>(_entries.Where(x => x.Scope == scope && (string.IsNullOrWhiteSpace(query) || x.Key.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Value.Contains(query, StringComparison.OrdinalIgnoreCase))).OrderByDescending(x => x.CreatedUtc).ToList());
}

public static class ForgeAiMemoryServiceCollectionExtensions
{
    /// <summary>
    /// Initializes or executes the AddForgeAiMemory operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The operation result.</returns>
    public static IServiceCollection AddForgeAiMemory(this IServiceCollection services) => services.AddSingleton<IForgeAiMemoryStore, InMemoryForgeAiMemoryStore>();
}
