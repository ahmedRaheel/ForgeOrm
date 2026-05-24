using ForgeORM.Core.Enums;
using ForgeORM.Core.GraphInsert.Models;

namespace ForgeORM.Core.GraphInsert.Interfaces
{
    public interface IForgeGraphExecutor
    {
        ForgeDatabaseProvider Provider { get; }

        ValueTask InsertGraphAsync<T>(
            T entity,
            ForgeGraphOptions options,
            CancellationToken cancellationToken = default)
            where T : class;

        ValueTask UpdateGraphAsync<T>(
            T entity,
            ForgeGraphOptions options,
            CancellationToken cancellationToken = default)
            where T : class;

        ValueTask DeleteGraphAsync<T>(
            T entity,
            ForgeGraphOptions options,
            CancellationToken cancellationToken = default)
            where T : class;
    }
}
