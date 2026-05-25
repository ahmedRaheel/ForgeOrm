using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.DataVirtualization;

public sealed record FederatedQueryPlan(IReadOnlyList<string> SourcePlans, string MergeStrategy, string SqlPreview);
