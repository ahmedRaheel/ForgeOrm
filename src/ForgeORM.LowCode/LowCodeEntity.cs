using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.LowCode;

public sealed record LowCodeEntity(string Name, IReadOnlyList<LowCodeField> Fields);
