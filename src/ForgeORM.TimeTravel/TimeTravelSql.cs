using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.TimeTravel;

public sealed record TimeTravelSql(string Sql, IReadOnlyDictionary<string,object> Parameters);
