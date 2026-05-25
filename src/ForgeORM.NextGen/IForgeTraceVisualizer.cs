
using System.Runtime.CompilerServices;
using System.Text;
using ForgeORM.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ForgeORM.NextGen;

public interface IForgeTraceVisualizer
/// <summary>
/// Defines the CreateTrace operation.
/// </summary>
/// <param name="sql">The sql value.</param>
/// <param name="parameters">The parameters value.</param>
/// <param name="providerName">The providerName value.</param>
/// <returns>The result of the CreateTrace operation.</returns>
{
    /// <summary>
    /// Defines the CreateTrace operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="providerName">The providerName value.</param>
    /// <returns>The result of the CreateTrace operation.</returns>
    ForgeTraceLink CreateTrace(string sql, object? parameters, string providerName);
}
