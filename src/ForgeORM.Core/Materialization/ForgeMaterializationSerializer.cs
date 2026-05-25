using System.Data.Common;
using System.Text;
using System.Text.Json;

namespace ForgeORM.Core.Materialization;

/// <summary>
/// Serialization helpers used by terminal APIs.
/// </summary>
public static class ForgeMaterializationSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static string ToJson(object value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    public static string ToCsv(IReadOnlyList<Dictionary<string, object?>> rows)
    {
        return new ForgeTabularResult
        {
            Columns = rows
                .SelectMany(x => x.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            Rows = rows
        }.ToCsv();
    }
}
