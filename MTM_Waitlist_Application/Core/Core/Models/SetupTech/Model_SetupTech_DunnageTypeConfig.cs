namespace Core.Models.SetupTech;

/// <summary>
/// Represents one dunnage type category that is enabled or disabled for the Setup Tech UI.
/// </summary>
public sealed class Model_SetupTech_DunnageTypeConfig
{
    /// <summary>The mirrored dunnage type identifier from the receiving catalog.</summary>
    public int DunnageTypeId { get; set; }

    /// <summary>The display name for the dunnage type.</summary>
    public string DunnageTypeName { get; set; } = string.Empty;

    /// <summary>Indicates whether the dunnage type should be shown in the UI.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>The display order used when rendering dunnage type tabs.</summary>
    public int DisplayOrder { get; set; }
}