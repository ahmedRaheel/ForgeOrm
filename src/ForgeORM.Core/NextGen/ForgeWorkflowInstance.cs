using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public sealed class ForgeWorkflowInstance
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public List<ForgeWorkflowStep> Steps { get; } = [];

    public ForgeWorkflowInstance AddStep(string name, string status = "Pending")
    {
        Steps.Add(new(name, status));
        return this;
    }
}
