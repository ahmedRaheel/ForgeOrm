using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.AI.Advanced;

public sealed record ForgeAiDiagnosticResult(string Severity, IReadOnlyList<string> Findings, IReadOnlyList<string> Fixes);
