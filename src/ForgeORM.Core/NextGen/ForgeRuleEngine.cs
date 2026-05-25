using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public static class ForgeRuleEngine
{
    public static bool Evaluate(decimal actual, ForgeRule rule)
        => rule.Operator switch
        {
            ">" => actual > rule.Value,
            ">=" => actual >= rule.Value,
            "<" => actual < rule.Value,
            "<=" => actual <= rule.Value,
            "=" => actual == rule.Value,
            _ => false
        };
}
