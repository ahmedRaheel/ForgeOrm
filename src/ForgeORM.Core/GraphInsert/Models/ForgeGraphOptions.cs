using ForgeORM.Core.Enums;

namespace ForgeORM.Core.GraphInsert.Models
{
    /// <summary>
    /// ForgeGraphOptions
    /// </summary>
    public sealed class ForgeGraphOptions
    {
        public bool IncludeChildren { get; set; } = true;
        public bool UseTransaction { get; set; } = true;
        public bool ReturnGeneratedKeys { get; set; } = true;
        public bool UseBulkWhenPossible { get; set; } = true;
        public int BatchSize { get; set; } = 500;
        public int MaxDepth { get; set; } = 5;

        public ForgeBulkStrategy Strategy { get; set; } = ForgeBulkStrategy.Auto;
        public ForgeChildSyncMode ChildSyncMode { get; set; } =
            ForgeChildSyncMode.InsertUpdateDeleteMissing;

        public ForgeDeleteMode DeleteMode { get; set; } = ForgeDeleteMode.HardDelete;
    }
}
