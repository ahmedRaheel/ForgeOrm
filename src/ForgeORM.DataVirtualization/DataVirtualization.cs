using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.DataVirtualization;

public sealed record VirtualDataSource(string Name, string Kind, string ConnectionName);
public sealed record FederatedQuery(string Name, string Query, IReadOnlyList<string> Sources);
public sealed record FederatedQueryPlan(IReadOnlyList<string> SourcePlans, string MergeStrategy, string SqlPreview);

public interface IForgeFederatedQueryPlanner
{
    FederatedQueryPlan Plan(FederatedQuery query, IReadOnlyList<VirtualDataSource> sources);
}

public sealed class ForgeFederatedQueryPlanner : IForgeFederatedQueryPlanner
{
    public FederatedQueryPlan Plan(FederatedQuery query, IReadOnlyList<VirtualDataSource> sources)
    {
        var sourcePlans = query.Sources.Select(s => $"Route '{query.Query}' to {s}").ToList();
        return new FederatedQueryPlan(sourcePlans, "Union/Merge by compatible columns", $"-- Federated query: {query.Name}\n{query.Query}");
    }
}

public static class ForgeDataVirtualizationServiceCollectionExtensions
{
    public static IServiceCollection AddForgeDataVirtualization(this IServiceCollection services) => services.AddSingleton<IForgeFederatedQueryPlanner, ForgeFederatedQueryPlanner>();
}
