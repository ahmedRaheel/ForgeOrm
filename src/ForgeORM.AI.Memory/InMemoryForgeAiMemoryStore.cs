using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.AI.Memory;

public sealed class InMemoryForgeAiMemoryStore : IForgeAiMemoryStore
{
    private readonly ConcurrentBag<ForgeMemoryEntry> _entries = [];
    /// <summary>
    /// Executes the RememberAsync operation.
    /// </summary>
    /// <param name="entry">The entry value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the RememberAsync operation.</returns>
    public ValueTask RememberAsync(ForgeMemoryEntry entry, CancellationToken cancellationToken = default) { _entries.Add(entry); return ValueTask.CompletedTask; }
    /// <summary>
    /// Executes the RecallAsync operation.
    /// </summary>
    /// <param name="scope">The scope value.</param>
    /// <param name="query">The query value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the RecallAsync operation.</returns>
    public ValueTask<IReadOnlyList<ForgeMemoryEntry>> RecallAsync(string scope, string? query = null, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<IReadOnlyList<ForgeMemoryEntry>>(_entries.Where(x => x.Scope == scope && (string.IsNullOrWhiteSpace(query) || x.Key.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Value.Contains(query, StringComparison.OrdinalIgnoreCase))).OrderByDescending(x => x.CreatedUtc).ToList());
}
