using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.AI.Memory;

public interface IForgeAiMemoryStore
/// <summary>
/// Defines the RememberAsync operation.
/// </summary>
/// <param name="entry">The entry value.</param>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the RememberAsync operation.</returns>
{
    /// <summary>
    /// Defines the RememberAsync operation.
    /// </summary>
    /// <param name="entry">The entry value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the RememberAsync operation.</returns>
    ValueTask RememberAsync(ForgeMemoryEntry entry, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the RecallAsync operation.
    /// </summary>
    /// <param name="scope">The scope value.</param>
    /// <param name="query">The query value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the RecallAsync operation.</returns>
    ValueTask<IReadOnlyList<ForgeMemoryEntry>> RecallAsync(string scope, string? query = null, CancellationToken cancellationToken = default);
}
