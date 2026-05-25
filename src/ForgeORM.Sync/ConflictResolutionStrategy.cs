using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Sync;

public enum ConflictResolutionStrategy
{
    ServerWins = 1,
    ClientWins = 2,
    Merge = 3,
    Manual = 4
}
