using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.LowCode;

public sealed record LowCodePage(string Name, string Route, string Entity, string Kind);
