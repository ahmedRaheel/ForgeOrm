using System;
using ForgeORM.Abstractions;
using System.Linq.Expressions;

namespace ForgeORM.QueryAst;

public sealed record ForgeRenderedSql(string Sql, object? Parameters = null);
