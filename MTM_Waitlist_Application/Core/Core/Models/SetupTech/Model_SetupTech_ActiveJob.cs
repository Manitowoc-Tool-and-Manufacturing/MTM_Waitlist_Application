namespace Core.Models.SetupTech;

/// <summary>
/// Represents the current active job configuration assigned to a workstation.
/// </summary>
public sealed class Model_SetupTech_ActiveJob
{
    /// <summary>The server-side identifier for the active job record.</summary>
    public int Id { get; set; }

    /// <summary>The workstation or workcenter identifier the active job is assigned to.</summary>
    public string WorkcenterId { get; set; } = string.Empty;

    /// <summary>The work order identifier currently active at the workstation.</summary>
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>The selected work order sequence number.</summary>
    public int SequenceNo { get; set; }

    /// <summary>The primary part number produced by the active job.</summary>
    public string PartId { get; set; } = string.Empty;

    /// <summary>The part type associated with the active job.</summary>
    public string PartType { get; set; } = string.Empty;

    /// <summary>The cached subordinate parts associated with the active job.</summary>
    public List<Model_SetupTech_SubordinatePart> SubordinateParts { get; set; } = [];

    /// <summary>The cached dunnage assignments associated with the active job.</summary>
    public List<Model_SetupTech_DunnageAssignment> DunnageAssignments { get; set; } = [];

    /// <summary>The user identifier of the setup technician who saved the job.</summary>
    public int SetupTechUserId { get; set; }

    /// <summary>The UTC timestamp when the job became active at the workstation.</summary>
    public DateTime ActiveSince { get; set; }
}