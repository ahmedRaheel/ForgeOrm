using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public sealed class ForgeGraphTraversalBuilder
{
    private readonly ForgeDb _db;
    internal ForgeGraphTraversalBuilder(ForgeDb db) => _db = db;
    public ForgeGraphTraversalFrom<TFrom> From<TFrom>(object id) => new(_db, typeof(TFrom).Name, id);
}
