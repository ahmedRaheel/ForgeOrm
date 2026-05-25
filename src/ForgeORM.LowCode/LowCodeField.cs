using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.LowCode;

public sealed record LowCodeField(string Name, string Type, bool Required = false, string? DisplayName = null);
