using SQLite;

namespace Data.Local;

/// <summary>
/// SQLite-mapped entity for caching workstation setup-tech job history.
/// </summary>
[Table("SetupTechJobHistoryCache")]
internal sealed class Entity_SetupTech_JobHistoryEntry
{
    /// <summary>The local cache identifier for the history row.</summary>
    [PrimaryKey, AutoIncrement]
    public int CacheId { get; set; }

    /// <summary>The server-side identifier for the history row.</summary>
    public int Id { get; set; }

    /// <summary>The workstation or workcenter identifier.</summary>
    public string WorkcenterId { get; set; } = string.Empty;

    /// <summary>The work order identifier captured in the history row.</summary>
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>The sequence number captured in the history row.</summary>
    public int SequenceNo { get; set; }

    /// <summary>The primary part number captured in the history row.</summary>
    public string PartId { get; set; } = string.Empty;

    /// <summary>The part type captured in the history row.</summary>
    public string PartType { get; set; } = string.Empty;

    /// <summary>The serialized subordinate-parts collection.</summary>
    public string SubordinatePartsJson { get; set; } = "[]";

    /// <summary>The serialized dunnage-assignments collection.</summary>
    public string DunnageAssignmentsJson { get; set; } = "[]";

    /// <summary>The user identifier of the setup technician who saved the job.</summary>
    public int SetupTechUserId { get; set; }

    /// <summary>The UTC timestamp when the job became active.</summary>
    public DateTime ActiveFrom { get; set; }

    /// <summary>The UTC timestamp when the job stopped being active.</summary>
    public DateTime ActiveUntil { get; set; }
}