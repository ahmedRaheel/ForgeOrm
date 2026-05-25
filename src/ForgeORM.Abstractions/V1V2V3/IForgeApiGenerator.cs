namespace ForgeORM.Abstractions;

public interface IForgeApiGenerator
/// <summary>
/// Defines the GenerateCrudApi operation.
/// </summary>
/// <param name="request">The request value.</param>
/// <returns>The result of the GenerateCrudApi operation.</returns>
{
    /// <summary>
    /// Defines the GenerateCrudApi operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <returns>The result of the GenerateCrudApi operation.</returns>
    IReadOnlyList<ForgeGeneratedFile> GenerateCrudApi(ForgeApiGenerationRequest request);
}
