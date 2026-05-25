using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using ForgeORM.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace ForgeORM.NextGen;

public sealed class ForgeShadowRow<T>
{
    /// <summary>
    /// Executes the ShadowProperty operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the ShadowProperty operation.</returns>
    public required T Entity { get; init; }
    /// <summary>
    /// Executes the ShadowProperty operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the ShadowProperty operation.</returns>
    public Dictionary<string, object?> ShadowValues { get; init; } = [];
    /// <summary>
    /// Executes the ShadowProperty operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the ShadowProperty operation.</returns>
    public object? ShadowProperty(string name) => ShadowValues.TryGetValue(name, out var value) ? value : null;
}
