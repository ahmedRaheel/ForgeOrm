using System.Linq.Expressions;
using ForgeORM.Core.Graph;
using System.Reflection;

namespace ForgeORM.Core;

public sealed record ForgeSyncResult(int Inserted, int Updated, int Deleted, int TotalInputRows);
