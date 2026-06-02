using SQLite;

namespace Data.Local;

/// <summary>
/// SQLite-mapped entity for caching setup-tech dunnage assignments locally.
/// </summary>
[Table("SetupTechDunnageAssignmentCache")]
internal sealed class Entity_SetupTech_DunnageAssignment
{
    /// <summary>The deterministic cache key for the assignment row.</summary>
    [PrimaryKey]
    public string CacheKey { get; set; } = string.Empty;

    /// <summary>The server-side identifier for the assignment row.</summary>
    [Indexed]
    public int AssignmentId { get; set; }

    /// <summary>The work order identifier associated with the assignment.</summary>
    [Indexed]
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>The sequence number associated with the assignment.</summary>
    [Indexed]
    public int SequenceNo { get; set; }

    /// <summary>The dunnage part identifier from the source catalog.</summary>
    public int DunnagePartId { get; set; }

    /// <summary>The display name for the dunnage part.</summary>
    public string DunnagePartName { get; set; } = string.Empty;

    /// <summary>The dunnage type identifier from the source catalog.</summary>
    public int DunnageTypeId { get; set; }

    /// <summary>The display name for the dunnage type.</summary>
    public string DunnageTypeName { get; set; } = string.Empty;

    /// <summary>The user identifier that last modified the assignment.</summary>
    public int LastModifiedByUserId { get; set; }
}