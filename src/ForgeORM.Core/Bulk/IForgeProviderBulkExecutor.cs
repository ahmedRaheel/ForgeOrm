using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace ForgeORM.Core;

internal interface IForgeProviderBulkExecutor
{
    ValueTask<int> ExecuteManyAsync<T>(
        DbConnection connection,
        string sql,
        IReadOnlyCollection<T> rows,
        CancellationToken cancellationToken);
}
