using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Identity;

public sealed record ForgePolicyDecision(bool Allowed, string Reason);
