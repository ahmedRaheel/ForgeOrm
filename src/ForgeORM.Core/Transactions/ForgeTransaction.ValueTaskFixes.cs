using System.Threading;
using System.Threading.Tasks;

namespace ForgeORM.Core;

public partial class ForgeTransaction
{
    public ValueTask CommitAsync(CancellationToken cancellationToken = default)
        => new(_transaction.CommitAsync(cancellationToken));

    public ValueTask RollbackAsync(CancellationToken cancellationToken = default)
        => new(_transaction.RollbackAsync(cancellationToken));
}
