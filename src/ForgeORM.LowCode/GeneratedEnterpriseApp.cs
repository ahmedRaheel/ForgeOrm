using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.LowCode;

public sealed record GeneratedEnterpriseApp(string Name, IReadOnlyList<LowCodeEntity> Entities, IReadOnlyList<string> Modules, IReadOnlyList<string> ApiRoutes);
