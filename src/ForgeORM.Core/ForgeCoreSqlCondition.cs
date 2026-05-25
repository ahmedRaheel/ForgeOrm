using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

internal sealed record ForgeCoreSqlCondition(string Sql, Dictionary<string, object?> Parameters);
