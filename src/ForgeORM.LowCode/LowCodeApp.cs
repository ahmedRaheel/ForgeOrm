using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.LowCode;

public sealed record LowCodeApp(string Name, IReadOnlyList<LowCodeEntity> Entities, IReadOnlyList<LowCodePage> Pages);
