using System.Collections.Concurrent;
using System.Text.Json;
using ForgeORM.Abstractions;
using AbstractionTenantContext = ForgeORM.Abstractions.ForgeTenantContext;
using AbstractionOutboxMessage = ForgeORM.Abstractions.ForgeOutboxMessage;

namespace ForgeORM.Core;

public sealed class StaticForgeTenantProvider : IForgeTenantProvider
{
    /// <summary>
    /// Executes the StaticForgeTenantProvider operation.
    /// </summary>
    /// <param name="tenantId">The tenantId value.</param>
    /// <param name="connectionString">The connectionString value.</param>
    /// <param name="schema">The schema value.</param>
    /// <returns>The result of the StaticForgeTenantProvider operation.</returns>
    public StaticForgeTenantProvider(string tenantId = "default", string? connectionString = null, string? schema = null)
        => Current = new AbstractionTenantContext(tenantId, connectionString, schema);

    public AbstractionTenantContext Current { get; }
}
