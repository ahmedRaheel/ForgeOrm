namespace ForgeORM.Abstractions;

public interface IForgeSchemaScaffolder
/// <summary>
/// Defines the ScaffoldAsync operation.
/// </summary>
/// <param name="request">The request value.</param>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the ScaffoldAsync operation.</returns>
{
    /// <summary>
    /// Defines the ScaffoldAsync operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ScaffoldAsync operation.</returns>
    ValueTask<IReadOnlyList<ForgeGeneratedFile>> ScaffoldAsync(ForgeScaffoldRequest request, CancellationToken cancellationToken = default);
}
