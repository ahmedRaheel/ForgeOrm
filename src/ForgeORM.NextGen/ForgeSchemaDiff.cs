using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using ForgeORM.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace ForgeORM.NextGen;

public sealed class ForgeSchemaDiff
{
    public bool HasChanges => Changes.Count > 0;
    public IReadOnlyList<string> Changes { get; init; } = [];
    public string FixScript { get; init; } = string.Empty;
}
