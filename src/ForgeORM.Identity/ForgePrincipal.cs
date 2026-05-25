using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Identity;

public sealed record ForgePrincipal(string UserId, IReadOnlyList<string> Roles, IReadOnlyDictionary<string,string> Claims);
