using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.AI.Advanced;

public interface IForgeAiDiagnostics
/// <summary>
/// Defines the Diagnose operation.
/// </summary>
/// <param name="sql">The sql value.</param>
/// <param name="elapsed">The elapsed value.</param>
/// <param name="rowCount">The rowCount value.</param>
/// <returns>The result of the Diagnose operation.</returns>
{
    /// <summary>
    /// Defines the Diagnose operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="elapsed">The elapsed value.</param>
    /// <param name="rowCount">The rowCount value.</param>
    /// <returns>The result of the Diagnose operation.</returns>
    ForgeAiDiagnosticResult Diagnose(string sql, TimeSpan elapsed, int rowCount);
}
