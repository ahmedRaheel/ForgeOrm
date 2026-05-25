using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace ForgeORM.Core;

public readonly record struct ForgeExpressionSqlKey(Type EntityType, string ProviderName, string ExpressionFingerprint);
