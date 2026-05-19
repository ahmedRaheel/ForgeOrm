namespace ForgeORM.RuntimeEmit;

public static class ForgeRuntimeEmitMode
{
    public static void UseRuntimeEmit()
    {
        ForgeORM.Core.ForgeSourceGeneratedRegistry.CompilationMode = ForgeORM.Core.ForgeOrmCompilationMode.RuntimeEmit;
    }
}
