using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public sealed record ForgeWorkflowStartResult(string Workflow, string ExecutionId, string Status);
