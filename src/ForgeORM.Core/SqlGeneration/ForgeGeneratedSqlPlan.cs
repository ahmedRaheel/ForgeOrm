using System.Collections.Concurrent;
using System.Text;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public sealed record ForgeGeneratedSqlPlan(string SelectById, string SelectAll, string Insert, string Update, string Delete);
