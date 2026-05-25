using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using ForgeORM.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace ForgeORM.NextGen;

public sealed class ForgeResiliencePolicy
{
    /// <summary>
    /// Executes the FromMilliseconds operation.
    /// </summary>
    /// <returns>The result of the FromMilliseconds operation.</returns>
    public int RetryCount { get; init; } = 0;
    /// <summary>
    /// Executes the FromMilliseconds operation.
    /// </summary>
    /// <returns>The result of the FromMilliseconds operation.</returns>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMilliseconds(100);
    /// <summary>
    /// Executes the FromSeconds operation.
    /// </summary>
    /// <returns>The result of the FromSeconds operation.</returns>
    public bool UseCircuitBreaker { get; init; }
    /// <summary>
    /// Executes the FromSeconds operation.
    /// </summary>
    /// <returns>The result of the FromSeconds operation.</returns>
    public TimeSpan CircuitBreakDuration { get; init; } = TimeSpan.FromSeconds(30);
}
