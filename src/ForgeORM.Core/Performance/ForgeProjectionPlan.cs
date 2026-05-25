using System.Collections.Concurrent;
using System.Linq.Expressions;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

internal sealed record ForgeProjectionPlan(Type SourceType, Type ProjectionType, string Fingerprint);
