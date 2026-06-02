using SQLite;

namespace Data.Local;

/// <summary>
/// SQLite-mapped entity for caching enabled setup-tech dunnage types locally.
/// </summary>
[Table("SetupTechDunnageTypeConfigCache")]
internal sealed class Entity_SetupTech_DunnageTypeConfig
{
    /// <summary>The mirrored dunnage type identifier used as the cache key.</summary>
    [PrimaryKey]
    public int DunnageTypeId { get; set; }

    /// <summary>The display name for the dunnage type.</summary>
    public string DunnageTypeName { get; set; } = string.Empty;

    /// <summary>Indicates whether the dunnage type is enabled in the setup-tech UI.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>The sort order used for the dunnage type.</summary>
    public int DisplayOrder { get; set; }
}