namespace DG.Tools.XrmMockup.Serialization
{
    /// <summary>
    /// DTO for serializing BooleanManagedProperty values in snapshots.
    /// BooleanManagedProperty has ExtensionDataObject that cannot be serialized by System.Text.Json.
    /// </summary>
    public class BooleanManagedPropertyDTO
    {
        public bool Value { get; set; }
        public bool CanBeChanged { get; set; }
        public string ManagedPropertyLogicalName { get; set; }
    }
}
