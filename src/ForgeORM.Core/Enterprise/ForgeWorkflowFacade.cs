using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public sealed class ForgeWorkflowFacade<TWorkflow>
{
    private readonly ForgeDb _db;
    internal ForgeWorkflowFacade(ForgeDb db) => _db = db;
    public ValueTask<ForgeWorkflowStartResult> StartAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new ForgeWorkflowStartResult(typeof(TWorkflow).Name, Guid.NewGuid().ToString("N"), "Started"));
}
