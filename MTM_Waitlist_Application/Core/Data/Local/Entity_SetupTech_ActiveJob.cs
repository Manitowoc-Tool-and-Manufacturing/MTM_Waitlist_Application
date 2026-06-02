using SQLite;

namespace Data.Local;

/// <summary>
/// SQLite-mapped entity for caching the active setup-tech job per workstation.
/// </summary>
[Table("SetupTechActiveJobCache")]
internal sealed class Entity_SetupTech_ActiveJob
{
    /// <summary>The workstation or workcenter identifier used as the cache key.</summary>
    [PrimaryKey]
    public string WorkcenterId { get; set; } = string.Empty;

    /// <summary>The server-side identifier for the active job record.</summary>
    public int Id { get; set; }

    /// <summary>The work order identifier assigned to the workstation.</summary>
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>The sequence number assigned to the workstation.</summary>
    public int SequenceNo { get; set; }

    /// <summary>The primary part number produced by the workstation.</summary>
    public string PartId { get; set; } = string.Empty;

    /// <summary>The part type associated with the active job.</summary>
    public string PartType { get; set; } = string.Empty;

    /// <summary>The serialized subordinate-parts collection.</summary>
    public string SubordinatePartsJson { get; set; } = "[]";

    /// <summary>The serialized dunnage-assignments collection.</summary>
    public string DunnageAssignmentsJson { get; set; } = "[]";

    /// <summary>The user identifier of the setup technician that saved the job.</summary>
    public int SetupTechUserId { get; set; }

    /// <summary>The UTC timestamp when the job became active.</summary>
    public DateTime ActiveSince { get; set; }
}