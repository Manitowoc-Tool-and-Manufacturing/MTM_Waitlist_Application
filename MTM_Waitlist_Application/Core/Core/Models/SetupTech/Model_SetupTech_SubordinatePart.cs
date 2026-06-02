namespace Core.Models.SetupTech;

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