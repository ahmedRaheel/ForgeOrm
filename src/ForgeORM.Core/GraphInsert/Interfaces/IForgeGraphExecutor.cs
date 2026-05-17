using ForgeORM.Core.Enums;
using ForgeORM.Core.GraphInsert.Models;

namespace ForgeORM.Core.GraphInsert.Interfaces
{
    public interface IForgeGraphExecutor
    {
        ForgeDatabaseProvider Provider { get; }

        Task InsertGraphAsync<T>(
            T entity,
            ForgeGraphOptions options,
            CancellationToken cancellationToken = default)
            where T : class;

        Task UpdateGraphAsync<T>(
            T entity,
            ForgeGraphOptions options,
            CancellationToken cancellationToken = default)
            where T : class;

        Task DeleteGraphAsync<T>(
            T entity,
            ForgeGraphOptions options,
            CancellationToken cancellationToken = default)
            where T : class;
    }
}
