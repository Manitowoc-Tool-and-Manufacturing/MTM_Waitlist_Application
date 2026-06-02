namespace MTM_Waitlist_Server.Api.Models.SetupTech;

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

/// <summary>
/// Represents a single dunnage item associated with a work order and sequence.
/// </summary>
public sealed class Model_SetupTech_DunnageAssignment
{
    /// <summary>The server-side identifier for the dunnage assignment row.</summary>
    public int AssignmentId { get; set; }

    /// <summary>The work order identifier the dunnage assignment belongs to.</summary>
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>The sequence number the dunnage assignment belongs to.</summary>
    public int SequenceNo { get; set; }

    /// <summary>The dunnage catalog part identifier from the source system.</summary>
    public int DunnagePartId { get; set; }

    /// <summary>The display name for the selected dunnage part.</summary>
    public string DunnagePartName { get; set; } = string.Empty;

    /// <summary>The dunnage type identifier mirrored from the receiving catalog.</summary>
    public int DunnageTypeId { get; set; }

    /// <summary>The display name for the dunnage type.</summary>
    public string DunnageTypeName { get; set; } = string.Empty;

    /// <summary>The user identifier that last modified the assignment row.</summary>
    public int LastModifiedByUserId { get; set; }
}

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

/// <summary>
/// Represents a cached subordinate part associated with a work order sequence.
/// </summary>
public sealed class Model_SetupTech_SubordinatePart
{
    /// <summary>The work order identifier that owns the subordinate part row.</summary>
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>The sequence number that owns the subordinate part row.</summary>
    public int SequenceNo { get; set; }

    /// <summary>The subordinate part identifier.</summary>
    public string SubPartId { get; set; } = string.Empty;

    /// <summary>The human-readable subordinate part description.</summary>
    public string? SubPartDesc { get; set; }

    /// <summary>The quantity required for the subordinate part.</summary>
    public decimal RequiredQty { get; set; }

    /// <summary>The quantity on hand captured when the part cache was refreshed.</summary>
    public decimal QtyOnHand { get; set; }
}