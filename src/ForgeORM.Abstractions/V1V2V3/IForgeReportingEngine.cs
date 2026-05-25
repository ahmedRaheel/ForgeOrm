namespace ForgeORM.Abstractions;

public interface IForgeReportingEngine
/// <summary>
/// Defines the Build operation.
/// </summary>
/// <param name="request">The request value.</param>
/// <param name="provider">The provider value.</param>
/// <returns>The result of the Build operation.</returns>
{
    /// <summary>
    /// Defines the Build operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the Build operation.</returns>
    ForgeReportSql Build(ForgeReportRequest request, string provider = "SqlServer");
}
