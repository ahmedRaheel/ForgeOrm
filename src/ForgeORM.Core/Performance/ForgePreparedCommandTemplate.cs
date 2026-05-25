using System.Collections.Concurrent;

namespace ForgeORM.Core.Performance;

internal sealed record ForgePreparedCommandTemplate(
    string Sql,
    string[] ParameterNames,
    Type? ParameterType,
    int? TimeoutSeconds);
