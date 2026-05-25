using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.DataVirtualization;

public enum FederatedExecutionMode
{
    Sequential = 1,
    Parallel = 2,
    Distributed = 3
}
