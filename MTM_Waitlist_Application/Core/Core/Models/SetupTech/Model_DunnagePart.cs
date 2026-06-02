namespace Core.Models.SetupTech;

/// <summary>
/// Represents one catalog item that can be assigned as dunnage during Setup Tech job setup.
/// </summary>
public sealed class Model_DunnagePart
{
    /// <summary>The unique dunnage part identifier from the source catalog.</summary>
    public int DunnagePartId { get; set; }

    /// <summary>The display name shown in the Setup Tech picker.</summary>
    public string DunnagePartName { get; set; } = string.Empty;

    /// <summary>The parent dunnage type identifier used for filtering.</summary>
    public int DunnageTypeId { get; set; }

    /// <summary>The parent dunnage type display name used for tabs and grouping.</summary>
    public string DunnageTypeName { get; set; } = string.Empty;
}