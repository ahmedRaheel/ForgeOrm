using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.AI.Advanced;

public interface IForgeAiCodeGenerator
/// <summary>
/// Defines the GenerateMinimalApiCrud operation.
/// </summary>
/// <param name="entityName">The entityName value.</param>
/// <param name="routePrefix">The routePrefix value.</param>
/// <returns>The result of the GenerateMinimalApiCrud operation.</returns>
{
    /// <summary>
    /// Defines the GenerateMinimalApiCrud operation.
    /// </summary>
    /// <param name="entityName">The entityName value.</param>
    /// <param name="routePrefix">The routePrefix value.</param>
    /// <returns>The result of the GenerateMinimalApiCrud operation.</returns>
    IReadOnlyList<ForgeGeneratedFile> GenerateMinimalApiCrud(string entityName, string routePrefix);
}
