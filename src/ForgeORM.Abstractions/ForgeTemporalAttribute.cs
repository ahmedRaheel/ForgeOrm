namespace ForgeORM.Abstractions;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ForgeTemporalAttribute : Attribute
{
    public string? HistoryTable { get; }
    public string PeriodStartColumn { get; }
    public string PeriodEndColumn { get; }

    public ForgeTemporalAttribute(
        string? historyTable = null,
        string periodStartColumn = "ValidFrom",
        string periodEndColumn = "ValidTo")
    {
        HistoryTable = historyTable;
        PeriodStartColumn = periodStartColumn;
        PeriodEndColumn = periodEndColumn;
    }
}
