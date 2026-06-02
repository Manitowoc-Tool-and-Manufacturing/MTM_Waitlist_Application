namespace Core.Models.SetupTech;

/// <summary>
/// Represents a prior active job entry archived for workstation history and analytics.
/// </summary>
public sealed class Model_SetupTech_JobHistoryEntry
{
    /// <summary>The server-side identifier for the archived job history row.</summary>
    public int Id { get; set; }

    /// <summary>The workstation or workcenter identifier the archived job belonged to.</summary>
    public string WorkcenterId { get; set; } = string.Empty;

    /// <summary>The work order identifier captured for the archived job.</summary>
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>The archived sequence number for the work order.</summary>
    public int SequenceNo { get; set; }

    /// <summary>The primary part number produced by the archived job.</summary>
    public string PartId { get; set; } = string.Empty;

    /// <summary>The part type associated with the archived job.</summary>
    public string PartType { get; set; } = string.Empty;

    /// <summary>The cached subordinate parts associated with the archived job.</summary>
    public List<Model_SetupTech_SubordinatePart> SubordinateParts { get; set; } = [];

    /// <summary>The cached dunnage assignments associated with the archived job.</summary>
    public List<Model_SetupTech_DunnageAssignment> DunnageAssignments { get; set; } = [];

    /// <summary>The user identifier of the setup technician who saved the archived job.</summary>
    public int SetupTechUserId { get; set; }

    /// <summary>The UTC timestamp when the archived job first became active.</summary>
    public DateTime ActiveFrom { get; set; }

    /// <summary>The UTC timestamp when the archived job stopped being active.</summary>
    public DateTime ActiveUntil { get; set; }
}