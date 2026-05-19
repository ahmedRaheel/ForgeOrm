using ForgeORM.Core.Enums;
using ForgeORM.Core.GraphInsert.Models;

namespace ForgeORM.Core.GraphInsert.Interfaces
{
    /// <summary>
    /// ForgeGraphStragegy Selector to insert Parent 
    /// child
    /// </summary>
    public interface IForgeGraphStrategySelector
    {
        ForgeBulkStrategy Select(
            ForgeDatabaseProvider provider,
            ForgeGraphOperation operation,
            int rowCount,
            ForgeGraphOptions options);
    }
}
