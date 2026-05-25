using System.Data;
using System.Text;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    private ForgeDbAiFacade? _ai;

    /// <summary>
    /// AI query facade exposed directly from db. Example: await db.AI.QueryAsync("Top 10 customers by revenue last month", o => o.TenantId = tenantId, ct).
    /// </summary>
    public ForgeDbAiFacade AI => _ai ??= new ForgeDbAiFacade(this);
}
