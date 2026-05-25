namespace ForgeORM.Core;

internal sealed class MySqlBulkExecutor : ForgeBulkExecutorBase
{
    public static readonly MySqlBulkExecutor Instance = new();
    private MySqlBulkExecutor() { }
    protected override string FormatParameterName(string name) => "@" + name;
}
