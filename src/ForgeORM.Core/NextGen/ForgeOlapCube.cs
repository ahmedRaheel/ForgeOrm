using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public sealed class ForgeOlapCube
{
    public string Name { get; }
    public List<ForgeOlapDimension> Dimensions { get; } = [];
    public List<ForgeOlapMeasure> Measures { get; } = [];

    public ForgeOlapCube(string name) => Name = name;

    public ForgeOlapCube Dimension(string name, string column)
    {
        Dimensions.Add(new(name, column));
        return this;
    }

    public ForgeOlapCube Measure(string name, string expression, string aggregate)
    {
        Measures.Add(new(name, expression, aggregate));
        return this;
    }
}
