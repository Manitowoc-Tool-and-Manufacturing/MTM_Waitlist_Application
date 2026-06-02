namespace Core.Models.InforVisual;

/// <summary>
/// Represents a workcenter resource loaded from Infor Visual.
/// </summary>
public sealed class Model_VisualWorkcenter
{
    /// <summary>The unique workcenter or resource identifier.</summary>
    public string WorkcenterId { get; set; } = string.Empty;

    /// <summary>The human-readable workcenter description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>The Infor Visual resource type code.</summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>The department identifier for the workcenter.</summary>
    public string DepartmentId { get; set; } = string.Empty;

    /// <summary>The schedule group identifier used by the resource.</summary>
    public string ScheduleGroup { get; set; } = string.Empty;
}