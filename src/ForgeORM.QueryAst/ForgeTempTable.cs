using System;
using ForgeORM.Abstractions;
using System.Linq.Expressions;

namespace ForgeORM.QueryAst;

public sealed class ForgeTempTable
{
    public required string Name { get; init; }
    public List<ForgeTempColumn> Columns { get; init; } = [];
    public List<string> PrimaryKeyColumns { get; init; } = [];
}
