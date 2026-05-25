using System.Collections.Concurrent;
using System.Text.Json;
using ForgeORM.Abstractions;
using AbstractionTenantContext = ForgeORM.Abstractions.ForgeTenantContext;
using AbstractionOutboxMessage = ForgeORM.Abstractions.ForgeOutboxMessage;

namespace ForgeORM.Core;

public sealed class SystemForgeAuditUserProvider : IForgeAuditUserProvider
{
    public string? UserId => Environment.UserName;
    public string? UserName => Environment.UserName;
}
