using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public static class ForgeAiCodingAgent
{
    public static ForgeAiAgentResult GenerateMinimalApi(string entity)
        => new("ApiAgent", $"Generate Minimal API for {entity}", $"app.MapGet(\"/{entity.ToLowerInvariant()}\", async (ForgeDbContext db) => await db.Query<{entity}>().ToListAsync());");
}
