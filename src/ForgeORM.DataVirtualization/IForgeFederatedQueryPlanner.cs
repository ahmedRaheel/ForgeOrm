using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.DataVirtualization;

public interface IForgeFederatedQueryPlanner
/// <summary>
/// Defines the Plan operation.
/// </summary>
/// <param name="query">The query value.</param>
/// <param name="sources">The sources value.</param>
/// <returns>The result of the Plan operation.</returns>
{
    /// <summary>
    /// Defines the Plan operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="sources">The sources value.</param>
    /// <returns>The result of the Plan operation.</returns>
    FederatedQueryPlan Plan(FederatedQuery query, IReadOnlyList<VirtualDataSource> sources);
    /// <summary>
    /// Defines the Plan operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="sources">The sources value.</param>
    /// <returns>The result of the Plan operation.</returns>
    FederatedPlanResult Plan(string query, IReadOnlyList<FederatedDataSource> sources);
    /// <summary>
    /// Defines the Plan operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <returns>The result of the Plan operation.</returns>
    FederatedPlanResult Plan(FederatedPlanRequest request);
}
