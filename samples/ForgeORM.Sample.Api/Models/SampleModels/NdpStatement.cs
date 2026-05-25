using ForgeORM.Abstractions;

public sealed class NdpStatement
{
    [ForgeColumn("current_financial_stat_year")]
    public int CurrentFinancialStatYear { get; set; }

    [ForgeColumn("EBITDA_To_Total_Indebtedness")]
    public decimal? EbitdaToTotalIndebtedness { get; set; }

    public string Sector { get; set; } = string.Empty;
}
