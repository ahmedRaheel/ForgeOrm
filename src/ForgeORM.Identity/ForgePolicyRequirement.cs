using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Identity;

public sealed record ForgePolicyRequirement(string Resource, string Action, string? TenantId = null);
