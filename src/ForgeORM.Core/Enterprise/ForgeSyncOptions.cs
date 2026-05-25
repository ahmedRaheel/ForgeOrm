using System.Linq.Expressions;
using ForgeORM.Core.Graph;
using System.Reflection;

namespace ForgeORM.Core;

public sealed class ForgeSyncOptions
{
    public bool InsertMissing { get; set; } = true;
    public bool UpdateExisting { get; set; } = true;
    public bool DeleteMissing { get; set; }
    public int BatchSize { get; set; } = 1000;
    public ForgeBulkStrategy Strategy { get; set; } = ForgeBulkStrategy.Auto;
}
