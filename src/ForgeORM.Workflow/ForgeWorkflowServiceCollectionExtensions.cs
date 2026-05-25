using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Workflow;

public static class ForgeWorkflowServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeWorkflow operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeWorkflow operation.</returns>
    public static IServiceCollection AddForgeWorkflow(this IServiceCollection services) => services.AddSingleton<IForgeWorkflowEngine, ForgeWorkflowEngine>();
}
