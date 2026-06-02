namespace Core.Models.InforVisual;

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