using System.Collections.Concurrent;
using System.Text.Json;
using ForgeORM.Abstractions;
using AbstractionTenantContext = ForgeORM.Abstractions.ForgeTenantContext;
using AbstractionOutboxMessage = ForgeORM.Abstractions.ForgeOutboxMessage;

namespace ForgeORM.Core;

public static class ForgeAudit
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="entity">The entity value.</param>
    /// <param name="userProvider">The userProvider value.</param>
    /// <returns>The result of the T operation.</returns>
    public static void StampCreate<T>(T entity, IForgeAuditUserProvider userProvider) where T : IForgeAuditable
    {
        entity.CreatedAt = DateTimeOffset.UtcNow;
        entity.CreatedBy = userProvider.UserName ?? userProvider.UserId;
        entity.IsDeleted = false;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="entity">The entity value.</param>
    /// <param name="userProvider">The userProvider value.</param>
    /// <returns>The result of the T operation.</returns>
    public static void StampUpdate<T>(T entity, IForgeAuditUserProvider userProvider) where T : IForgeAuditable
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = userProvider.UserName ?? userProvider.UserId;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="entity">The entity value.</param>
    /// <param name="userProvider">The userProvider value.</param>
    /// <returns>The result of the T operation.</returns>
    public static void SoftDelete<T>(T entity, IForgeAuditUserProvider userProvider) where T : IForgeAuditable
    {
        entity.IsDeleted = true;
        StampUpdate(entity, userProvider);
    }
}
