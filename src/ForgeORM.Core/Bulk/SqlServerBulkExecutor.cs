namespace ForgeORM.Core;

internal sealed class SqlServerBulkExecutor : ForgeBulkExecutorBase
{
    public static readonly SqlServerBulkExecutor Instance = new();
    private SqlServerBulkExecutor() { }
    protected override string FormatParameterName(string name) => "@" + name;
}
