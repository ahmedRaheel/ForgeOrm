namespace ForgeORM.SourceGenerated;

public static class ForgeSourceGeneratedMode
{
    public static void UseSourceGenerated()
    {
        ForgeORM.Core.ForgeSourceGeneratedRegistry.CompilationMode = ForgeORM.Core.ForgeOrmCompilationMode.SourceGenerated;
    }

    public static void UseAuto()
    {
        ForgeORM.Core.ForgeSourceGeneratedRegistry.CompilationMode = ForgeORM.Core.ForgeOrmCompilationMode.Auto;
    }
}
