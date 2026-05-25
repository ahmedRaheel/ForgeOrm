using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Observability metric snapshot.
/// </summary>
public sealed record ForgeDatabaseMetric(
    string Name,
    double Value,
    string Unit,
    DateTimeOffset CapturedAtUtc);
