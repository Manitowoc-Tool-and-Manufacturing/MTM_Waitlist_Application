namespace Core.Models.InforVisual;

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