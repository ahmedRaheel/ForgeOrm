using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.AI.Advanced;

public interface IForgeAiOptimizer
/// <summary>
/// Defines the Optimize operation.
/// </summary>
/// <param name="request">The request value.</param>
/// <returns>The result of the Optimize operation.</returns>
{
    /// <summary>
    /// Defines the Optimize operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <returns>The result of the Optimize operation.</returns>
    ForgeAiOptimizationResult Optimize(ForgeAiOptimizationRequest request);
}
