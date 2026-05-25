using System.Data;
using System.Data.Common;
using ForgeORM.Core.Performance;

namespace ForgeORM.Core;

internal readonly record struct ForgeBatchCommand(string Sql, object? Parameters, CommandType CommandType);
