using SQLite;

namespace Data.Local;

/// <summary>
/// SQLite-mapped entity for caching Infor Visual workcenters locally.
/// </summary>
[Table("VisualWorkcenterCache")]
internal sealed class Entity_VisualWorkcenter
{
    /// <summary>The unique workcenter identifier.</summary>
    [PrimaryKey]
    public string WorkcenterId { get; set; } = string.Empty;

    /// <summary>The display description of the workcenter.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>The Infor Visual resource type code.</summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>The workcenter department identifier.</summary>
    public string DepartmentId { get; set; } = string.Empty;

    /// <summary>The workcenter schedule group identifier.</summary>
    public string ScheduleGroup { get; set; } = string.Empty;
}