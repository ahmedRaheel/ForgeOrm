namespace ForgeORM.Core;

/// <summary>
/// Runtime configuration for ForgeORM compilation strategy. NativeAOT applications can explicitly choose
/// SourceGenerated; regular JIT applications can choose Auto or RuntimeEmit.
/// </summary>
public sealed class ForgeCompilationOptions
{
    public ForgeOrmCompilationMode Mode { get; set; } = ForgeOrmCompilationMode.Auto;
}
