namespace ForgeORM.Security;

public sealed class ForgeDataMasker : IForgeDataMasker
{
    /// <summary>
    /// Executes the MaskEmail operation.
    /// </summary>
    /// <param name="email">The email value.</param>
    /// <returns>The result of the MaskEmail operation.</returns>
    public string MaskEmail(string email)
    {
        var parts = email.Split('@');
        return parts.Length == 2 ? $"{Mask(parts[0], 1, 1)}@{parts[1]}" : Mask(email);
    }
    /// <summary>
    /// Executes the MaskPhone operation.
    /// </summary>
    /// <param name="phone">The phone value.</param>
    /// <returns>The result of the MaskPhone operation.</returns>
    public string MaskPhone(string phone) => Mask(phone, 2, 2);
    /// <summary>
    /// Executes the Mask operation.
    /// </summary>
    /// <param name="value">The value value.</param>
    /// <param name="visibleStart">The visibleStart value.</param>
    /// <param name="visibleEnd">The visibleEnd value.</param>
    /// <returns>The result of the Mask operation.</returns>
    public string Mask(string value, int visibleStart = 2, int visibleEnd = 2)
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (value.Length <= visibleStart + visibleEnd) return new string('*', value.Length);
        return value[..visibleStart] + new string('*', value.Length - visibleStart - visibleEnd) + value[^visibleEnd..];
    }
}
