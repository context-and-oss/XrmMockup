namespace DG.Tools.XrmMockup.Serialization
{
    /// <summary>Sequence state stored alongside entity data in environment snapshots.</summary>
    public sealed class AutoNumberStateDTO
    {
        public string EntityName { get; set; }
        public string AttributeName { get; set; }
        public long Seed { get; set; }
        public long NextValue { get; set; }
        public bool Exhausted { get; set; }

        internal AutoNumberStateDTO Clone() => (AutoNumberStateDTO)MemberwiseClone();
    }
}
