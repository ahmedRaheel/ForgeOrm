using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using ForgeORM.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace ForgeORM.NextGen;

public interface IForgeSmartQuery<T>
/// <summary>
/// Defines the WhereSql operation.
/// </summary>
/// <param name="sql">The sql value.</param>
/// <returns>The result of the WhereSql operation.</returns>
{
    /// <summary>
    /// Defines the WhereSql operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the WhereSql operation.</returns>
    IForgeSmartQuery<T> WhereSql(FormattableString sql);
    /// <summary>
    /// Defines the WithPolicy operation.
    /// </summary>
    /// <param name="policy">The policy value.</param>
    /// <returns>The result of the WithPolicy operation.</returns>
    IForgeSmartQuery<T> WithPolicy(ForgeResiliencePolicy policy);
    /// <summary>
    /// Defines the AsCached operation.
    /// </summary>
    /// <param name="duration">The duration value.</param>
    /// <param name="key">The key value.</param>
    /// <returns>The result of the AsCached operation.</returns>
    IForgeSmartQuery<T> AsCached(TimeSpan duration, string? key = null);
    /// <summary>
    /// Defines the Mock operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the Mock operation.</returns>
    IForgeSmartQuery<T> Mock(IEnumerable<T> rows);
    /// <summary>
    /// Defines the IncludeGraph operation.
    /// </summary>
    /// <param name="maxDepth">The maxDepth value.</param>
    /// <returns>The result of the IncludeGraph operation.</returns>
    IForgeSmartQuery<T> IncludeGraph(int maxDepth = 2);
    /// <summary>
    /// Defines the ShadowProperty operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the ShadowProperty operation.</returns>
    IForgeSmartQuery<T> ShadowProperty(string name);

/// <summary>

/// Defines the ExecuteTransparent operation.

/// </summary>

/// <returns>The result of the ExecuteTransparent operation.</returns>

    /// <summary>
    /// Defines the ExecuteTransparent operation.
    /// </summary>
    /// <returns>The result of the ExecuteTransparent operation.</returns>
    ForgeTransparentCommand ExecuteTransparent();
    /// <summary>
    /// Defines the Explain operation.
    /// </summary>
    /// <returns>The result of the Explain operation.</returns>
    ForgeExplainResult Explain();
    /// <summary>
    /// Defines the ExplainAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ExplainAsync operation.</returns>
    ValueTask<ForgeExplainResult> ExplainAsync(CancellationToken cancellationToken = default);

/// <summary>

/// Defines the TShape operation.

/// </summary>

/// <typeparam name="TShape">The type used by the operation.</typeparam>

/// <returns>The result of the TShape operation.</returns>

    /// <summary>
    /// Defines the TShape operation.
    /// </summary>
    /// <typeparam name="TShape">The type used by the operation.</typeparam>
    /// <returns>The result of the TShape operation.</returns>
    IReadOnlyList<TShape> ToShape<TShape>();
    /// <summary>
    /// Defines the TShape operation.
    /// </summary>
    /// <typeparam name="TShape">The type used by the operation.</typeparam>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the TShape operation.</returns>
    ValueTask<IReadOnlyList<TShape>> ToShapeAsync<TShape>(CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the TShape operation.
    /// </summary>
    /// <typeparam name="TShape">The type used by the operation.</typeparam>
    /// <returns>The result of the TShape operation.</returns>
    IReadOnlyList<TShape> MapStatic<TShape>();
    /// <summary>
    /// Defines the TShape operation.
    /// </summary>
    /// <typeparam name="TShape">The type used by the operation.</typeparam>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the TShape operation.</returns>
    ValueTask<IReadOnlyList<TShape>> MapStaticAsync<TShape>(CancellationToken cancellationToken = default);

/// <summary>

/// Defines the IntoJsonDocument operation.

/// </summary>

/// <returns>The result of the IntoJsonDocument operation.</returns>

    /// <summary>
    /// Defines the IntoJsonDocument operation.
    /// </summary>
    /// <returns>The result of the IntoJsonDocument operation.</returns>
    JsonDocument IntoJsonDocument();
    /// <summary>
    /// Defines the IntoJsonDocumentAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the IntoJsonDocumentAsync operation.</returns>
    ValueTask<JsonDocument> IntoJsonDocumentAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the IntoJson operation.
    /// </summary>
    /// <returns>The result of the IntoJson operation.</returns>
    string IntoJson();
    /// <summary>
    /// Defines the IntoJsonAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the IntoJsonAsync operation.</returns>
    ValueTask<string> IntoJsonAsync(CancellationToken cancellationToken = default);

/// <summary>

/// Defines the StreamAllAsync operation.

/// </summary>

/// <param name="cancellationToken">The cancellationToken value.</param>

/// <returns>The result of the StreamAllAsync operation.</returns>

    /// <summary>
    /// Defines the StreamAllAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the StreamAllAsync operation.</returns>
    IAsyncEnumerable<T> StreamAllAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the ToList operation.
    /// </summary>
    /// <returns>The result of the ToList operation.</returns>
    IReadOnlyList<T> ToList();
    /// <summary>
    /// Defines the ToListAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ToListAsync operation.</returns>
    ValueTask<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default);
}
