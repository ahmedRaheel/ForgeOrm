using System;
using ForgeORM.Abstractions;
using System.Linq.Expressions;

namespace ForgeORM.QueryAst;

public sealed record ForgeTempColumn(string Name, string DbType, bool Nullable);
