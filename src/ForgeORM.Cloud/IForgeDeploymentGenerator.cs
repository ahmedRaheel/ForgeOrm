using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Cloud;

public interface IForgeDeploymentGenerator
/// <summary>
/// Defines the Generate operation.
/// </summary>
/// <param name="request">The request value.</param>
/// <returns>The result of the Generate operation.</returns>
{
    /// <summary>
    /// Defines the Generate operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <returns>The result of the Generate operation.</returns>
    CloudDeploymentArtifacts Generate(CloudDeploymentRequest request);
}
