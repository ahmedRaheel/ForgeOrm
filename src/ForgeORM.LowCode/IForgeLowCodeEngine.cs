using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.LowCode;

public interface IForgeLowCodeEngine
/// <summary>
/// Defines the GenerateErp operation.
/// </summary>
/// <param name="businessDomain">The businessDomain value.</param>
/// <param name="modules">The modules value.</param>
/// <returns>The result of the GenerateErp operation.</returns>
{
    /// <summary>
    /// Defines the GenerateErp operation.
    /// </summary>
    /// <param name="businessDomain">The businessDomain value.</param>
    /// <param name="modules">The modules value.</param>
    /// <returns>The result of the GenerateErp operation.</returns>
    GeneratedEnterpriseApp GenerateErp(string businessDomain, IReadOnlyList<string> modules);
    /// <summary>
    /// Defines the GenerateMinimalApi operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <returns>The result of the GenerateMinimalApi operation.</returns>
    string GenerateMinimalApi(LowCodeEntity entity);
    /// <summary>
    /// Defines the GenerateReactForm operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <returns>The result of the GenerateReactForm operation.</returns>
    string GenerateReactForm(LowCodeEntity entity);
}
