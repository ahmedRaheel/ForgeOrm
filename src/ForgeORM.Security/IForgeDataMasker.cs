namespace ForgeORM.Security;

public interface IForgeDataMasker
/// <summary>
/// Defines the MaskEmail operation.
/// </summary>
/// <param name="email">The email value.</param>
/// <returns>The result of the MaskEmail operation.</returns>
{
    /// <summary>
    /// Defines the MaskEmail operation.
    /// </summary>
    /// <param name="email">The email value.</param>
    /// <returns>The result of the MaskEmail operation.</returns>
    string MaskEmail(string email);
    /// <summary>
    /// Defines the MaskPhone operation.
    /// </summary>
    /// <param name="phone">The phone value.</param>
    /// <returns>The result of the MaskPhone operation.</returns>
    string MaskPhone(string phone);
    /// <summary>
    /// Defines the Mask operation.
    /// </summary>
    /// <param name="value">The value value.</param>
    /// <param name="visibleStart">The visibleStart value.</param>
    /// <param name="visibleEnd">The visibleEnd value.</param>
    /// <returns>The result of the Mask operation.</returns>
    string Mask(string value, int visibleStart = 2, int visibleEnd = 2);
}
