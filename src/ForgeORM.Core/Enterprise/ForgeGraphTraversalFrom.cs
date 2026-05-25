using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public sealed class ForgeGraphTraversalFrom<TFrom>
{
    private readonly ForgeDb _db;
    private readonly string _from;
    private readonly object _fromId;
    private string? _edge;
    internal ForgeGraphTraversalFrom(ForgeDb db, string from, object fromId) { _db = db; _from = from; _fromId = fromId; }
    public ForgeGraphTraversalFrom<TFrom> Traverse(string relationship) { _edge = relationship; return this; }
    public ForgeGraphPathQuery<TTo> ShortestPathTo<TTo>(object id) => new(_db, _from, _fromId, _edge, typeof(TTo).Name, id);
}
