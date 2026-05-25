using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using ForgeORM.Abstractions;
using ForgeORM.QueryAst.Artifacts;

namespace ForgeORM.SchemaOps;

public interface IForgeArtifactManager
/// <summary>
/// Defines the EnsureHistoryTableAsync operation.
/// </summary>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the EnsureHistoryTableAsync operation.</returns>
{
    /// <summary>
    /// Defines the EnsureHistoryTableAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the EnsureHistoryTableAsync operation.</returns>
    ValueTask EnsureHistoryTableAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the CreateOrUpdateAsync operation.
    /// </summary>
    /// <param name="artifact">The artifact value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the CreateOrUpdateAsync operation.</returns>
    ValueTask<ForgeArtifactApplyResult> CreateOrUpdateAsync(ForgeDbArtifact artifact, CancellationToken cancellationToken = default);
}
