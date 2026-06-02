namespace MTM_Waitlist_Server.Api.Models.InforVisual;

/// <summary>
/// Represents the header details returned for an Infor Visual work order.
/// </summary>
public sealed class Model_VisualWorkOrderHeader
{
    /// <summary>The work order identifier shown to users and scanners.</summary>
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>The primary part number being produced by the work order.</summary>
    public string PartId { get; set; } = string.Empty;

    /// <summary>The human-readable description of the part.</summary>
    public string PartDescription { get; set; } = string.Empty;

    /// <summary>The Infor Visual part type for the work order part.</summary>
    public string PartType { get; set; } = string.Empty;

    /// <summary>The current work order status code returned by Infor Visual.</summary>
    public string WorkOrderStatus { get; set; } = string.Empty;

    /// <summary>The desired production quantity for the work order.</summary>
    public decimal DesiredQty { get; set; }

    /// <summary>The requested completion or want date for the work order.</summary>
    public DateTime? WantDate { get; set; }

    /// <summary>The site or warehouse identifier associated with the work order.</summary>
    public string SiteId { get; set; } = string.Empty;
}

/// <summary>
/// Represents a single sequence on an Infor Visual work order.
/// </summary>
public sealed class Model_VisualWorkOrderSequence
{
    /// <summary>The work order identifier that owns this sequence.</summary>
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>The numeric sequence value shown in the UI as Seq or Sequence.</summary>
    public int SequenceNo { get; set; }

    /// <summary>The workcenter or resource currently assigned to this sequence.</summary>
    public string WorkcenterId { get; set; } = string.Empty;

    /// <summary>The human-readable description of the assigned workcenter.</summary>
    public string WorkcenterDescription { get; set; } = string.Empty;

    /// <summary>The current sequence status code returned by Infor Visual.</summary>
    public string SequenceStatus { get; set; } = string.Empty;

    /// <summary>Indicates whether setup has already been marked complete for this sequence.</summary>
    public bool SetupCompleted { get; set; }

    /// <summary>The quantity already completed on this sequence.</summary>
    public decimal CompletedQty { get; set; }

    /// <summary>The target quantity expected at the end of this sequence.</summary>
    public decimal TargetQty { get; set; }

    /// <summary>The scheduled start timestamp for this sequence.</summary>
    public DateTime? SchedStart { get; set; }

    /// <summary>The scheduled finish timestamp for this sequence.</summary>
    public DateTime? SchedFinish { get; set; }
}

/// <summary>
/// Represents one available workcenter returned from Infor Visual.
/// </summary>
public sealed class Model_VisualWorkcenter
{
    /// <summary>The unique workcenter or resource identifier.</summary>
    public string WorkcenterId { get; set; } = string.Empty;

    /// <summary>The human-readable workcenter description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>The Visual resource type code.</summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>The department identifier for the workcenter.</summary>
    public string DepartmentId { get; set; } = string.Empty;

    /// <summary>The scheduling group identifier for the workcenter.</summary>
    public string ScheduleGroup { get; set; } = string.Empty;
}

/// <summary>
/// Represents a subordinate or component part line returned for a work order sequence.
/// </summary>
public sealed class Model_Visual_SubordinatePart
{
    /// <summary>The work order identifier that owns the subordinate part line.</summary>
    public string WorkOrderId { get; set; } = string.Empty;

    /// <summary>The sequence number the subordinate part line belongs to.</summary>
    public int SequenceNo { get; set; }

    /// <summary>The subordinate component part identifier.</summary>
    public string PartId { get; set; } = string.Empty;

    /// <summary>The human-readable description of the subordinate part.</summary>
    public string PartDescription { get; set; } = string.Empty;

    /// <summary>The required quantity for the subordinate part on the job.</summary>
    public decimal RequiredQty { get; set; }

    /// <summary>The quantity on hand captured for the subordinate part.</summary>
    public decimal QtyOnHand { get; set; }
}