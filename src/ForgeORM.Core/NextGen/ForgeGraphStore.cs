using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public sealed class ForgeGraphStore
{
    public List<ForgeGraphNode> Nodes { get; } = [];
    public List<ForgeGraphEdge> Edges { get; } = [];

    public ForgeGraphStore AddNode(string id, string label)
    {
        Nodes.Add(new(id, label));
        return this;
    }

    public ForgeGraphStore AddEdge(string from, string to, string type)
    {
        Edges.Add(new(from, to, type));
        return this;
    }

    public IReadOnlyList<ForgeGraphNode> Neighbors(string id)
    {
        var ids = Edges.Where(e => e.From == id).Select(e => e.To).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Nodes.Where(n => ids.Contains(n.Id)).ToList();
    }
}
