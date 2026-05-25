using System.Collections;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace ForgeORM.DataFrame;

internal sealed class ForgeObjectEqualityComparer : IEqualityComparer<object?>
{
    public static readonly ForgeObjectEqualityComparer Instance = new();

    public new bool Equals(object? x, object? y)
        => string.Equals(Convert.ToString(x, CultureInfo.InvariantCulture), Convert.ToString(y, CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);

    public int GetHashCode(object? obj)
        => StringComparer.OrdinalIgnoreCase.GetHashCode(Convert.ToString(obj, CultureInfo.InvariantCulture) ?? string.Empty);
}
