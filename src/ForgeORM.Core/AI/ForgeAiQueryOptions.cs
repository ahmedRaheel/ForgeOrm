using System.Data;
using System.Text;

namespace ForgeORM.Core;

/// <summary>
/// Options used by db.AI.QueryAsync to keep natural-language SQL generation tenant-aware and safe by default.
/// </summary>
public sealed class ForgeAiQueryOptions
{
    /// <summary>Optional tenant id appended as a TenantId filter when the generated SQL can be safely constrained.</summary>
    public object? TenantId { get; set; }

    /// <summary>Optional entity/table name. Defaults to Orders for revenue/order-style prompts.</summary>
    public string? Entity { get; set; }

    /// <summary>Maximum number of rows returned by the generated SQL. Defaults to 100.</summary>
    public int Take { get; set; } = 100;

    /// <summary>Rejects non-read SQL. Enabled by default.</summary>
    public bool SafeMode { get; set; } = true;

    /// <summary>When true, result includes generated SQL and diagnostics.</summary>
    public bool IncludeSql { get; set; } = true;
}
