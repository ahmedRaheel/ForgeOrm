namespace ForgeORM.Abstractions;

public interface IForgeAiQueryClient
/// <summary>
/// Defines the GenerateSqlAsync operation.
/// </summary>
/// <param name="request">The request value.</param>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the GenerateSqlAsync operation.</returns>
{
    /// <summary>
    /// Defines the GenerateSqlAsync operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the GenerateSqlAsync operation.</returns>
    ValueTask<ForgeAiQueryResult> GenerateSqlAsync(ForgeAiQueryRequest request, CancellationToken cancellationToken = default);
}
