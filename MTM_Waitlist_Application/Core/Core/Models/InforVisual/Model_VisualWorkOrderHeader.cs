namespace Core.Models.InforVisual;

/// <summary>
/// Represents the header details for a work order retrieved from Infor Visual.
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