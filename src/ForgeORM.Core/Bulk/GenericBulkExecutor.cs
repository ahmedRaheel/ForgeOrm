namespace ForgeORM.Core;

internal sealed class GenericBulkExecutor : ForgeBulkExecutorBase
{
    public static readonly GenericBulkExecutor Instance = new();
    private GenericBulkExecutor() { }
}
