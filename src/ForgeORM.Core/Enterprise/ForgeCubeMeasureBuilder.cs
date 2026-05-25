using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public sealed class ForgeCubeMeasureBuilder<T>
{
    private readonly string _alias;
    internal ForgeCubeMeasureBuilder(string alias) => _alias = alias;
    public ForgeCubeMeasure Sum(Expression<Func<T, decimal>> selector) => new($"SUM({ForgeExpressionTranslator.MemberName(selector)}) AS {_alias}");
    public ForgeCubeMeasure Count() => new($"COUNT(1) AS {_alias}");
    public ForgeCubeMeasure Average(Expression<Func<T, decimal>> selector) => new($"AVG({ForgeExpressionTranslator.MemberName(selector)}) AS {_alias}");
    public ForgeCubeMeasure Min(Expression<Func<T, decimal>> selector) => new($"MIN({ForgeExpressionTranslator.MemberName(selector)}) AS {_alias}");
    public ForgeCubeMeasure Max(Expression<Func<T, decimal>> selector) => new($"MAX({ForgeExpressionTranslator.MemberName(selector)}) AS {_alias}");
}
