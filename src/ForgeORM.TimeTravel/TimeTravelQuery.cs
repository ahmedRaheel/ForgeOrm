using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.TimeTravel;

public sealed record TimeTravelQuery(string Entity, DateTimeOffset AsOfUtc, string? Filter = null);
