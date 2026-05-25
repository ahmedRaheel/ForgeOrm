namespace ForgeORM.Core.Graph.Strategies;

/// <summary>
/// Selects best execution strategy per provider.
/// </summary>
public interface IForgeGraphStrategySelector
{
    ForgeBulkStrategy Select(
        ForgeDatabaseProvider provider,
        ForgeGraphOperation operation,
        int rowCount,
        ForgeGraphOptions options);
}