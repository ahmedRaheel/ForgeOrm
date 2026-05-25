using System.Text;

namespace ForgeORM.Analytics.Reporting;

/// <summary>
/// Export helpers for report rows.
/// </summary>
public static class ForgeReportExport
{
    public static byte[] ToCsvBytes(IEnumerable<IDictionary<string, object?>> rows)
    {
        var csv = ToCsvText(rows);
        return Encoding.UTF8.GetBytes(csv);
    }

    public static string ToCsvText(IEnumerable<IDictionary<string, object?>> rows)
    {
        var list = rows.ToList();
        if (list.Count == 0)
        {
            return string.Empty;
        }

        var columns = list.SelectMany(x => x.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', columns.Select(EscapeCsv)));

        foreach (var row in list)
        {
            sb.AppendLine(string.Join(',', columns.Select(column =>
            {
                row.TryGetValue(column, out var value);
                return EscapeCsv(value?.ToString() ?? string.Empty);
            })));
        }

        return sb.ToString();
    }

    public static byte[] ToExcelXmlBytes(IEnumerable<IDictionary<string, object?>> rows, string worksheetName = "Report")
    {
        var list = rows.ToList();
        var columns = list.SelectMany(x => x.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\"?>");
        sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
        sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
        sb.AppendLine($"<Worksheet ss:Name=\"{EscapeXml(worksheetName)}\"><Table>");
        sb.AppendLine("<Row>" + string.Join(string.Empty, columns.Select(c => Cell(c))) + "</Row>");

        foreach (var row in list)
        {
            sb.AppendLine("<Row>" + string.Join(string.Empty, columns.Select(column =>
            {
                row.TryGetValue(column, out var value);
                return Cell(value?.ToString() ?? string.Empty);
            })) + "</Row>");
        }

        sb.AppendLine("</Table></Worksheet></Workbook>");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string Cell(string value) => $"<Cell><Data ss:Type=\"String\">{EscapeXml(value)}</Data></Cell>";

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    private static string EscapeXml(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
