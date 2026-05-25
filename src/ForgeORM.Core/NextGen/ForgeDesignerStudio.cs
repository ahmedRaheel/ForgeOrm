using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public static class ForgeDesignerStudio
{
    public static ForgeDesignerArtifact QueryDesigner(string name, string sql)
        => new("QueryDesigner", name, $$"""{"sql": "{{sql.Replace("\"", "\\\"")}}"}""");
}
