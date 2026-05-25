using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public sealed record ForgeGraphPath(IReadOnlyList<ForgeGraphPathNode> Nodes, IReadOnlyList<ForgeGraphPathEdge> Edges);
