using System.Collections;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace ForgeORM.DataFrame;

/// <summary>
/// Lightweight plot metadata produced by ForgeDataFrame.Plot().
/// </summary>
public sealed record ForgePlotSpec(string Kind, string X, string Y, string Title, IReadOnlyList<IDictionary<string, object?>> Data);
