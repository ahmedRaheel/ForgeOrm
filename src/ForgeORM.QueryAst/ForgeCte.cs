using System;
using ForgeORM.Abstractions;
using System.Linq.Expressions;

namespace ForgeORM.QueryAst;

public sealed record ForgeCte(string Name, string Sql);
