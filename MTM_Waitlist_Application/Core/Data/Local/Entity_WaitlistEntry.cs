using Core.Enums.Waitlist;
using SQLite;

namespace Data.Local;

/// <summary>
/// SQLite-mapped entity for caching a waitlist entry on-device.
/// Mirrors <see cref="Core.Models.Waitlist.Model_WaitlistEntry"/>.
/// Used by <see cref="Repositories.Waitlist.Repository_WaitlistEntryLocal"/> via
/// <see cref="LocalDbContext"/> when the device is offline.
/// </summary>
[Table("WaitlistEntries")]
internal sealed class Entity_WaitlistEntry
{
    /// <summary>Unique identifier — matches the server-side primary key.</summary>
    [PrimaryKey]
    public int Id { get; set; }

    /// <summary>The name of the workcenter submitting the request.</summary>
    public string WorkcenterName { get; set; } = string.Empty;

    /// <summary>
    /// The category of logistics request. Stored as an integer
    /// (the underlying enum value) by sqlite-net-pcl.
    /// </summary>
    public Enum_WaitlistRequestType RequestType { get; set; }

    /// <summary>
    /// Current lifecycle state. Stored as an integer by sqlite-net-pcl.
    /// </summary>
    public Enum_WaitlistStatus Status { get; set; }

    /// <summary>Sort priority. 1 = highest, 10 = lowest.</summary>
    public int Priority { get; set; }

    /// <summary>Optional free-text remarks.</summary>
    public string? Notes { get; set; }

    /// <summary>UTC — when the request was submitted.</summary>
    public DateTime RequestedAt { get; set; }

    /// <summary>UTC — estimated or confirmed fulfillment time. Nullable.</summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>UTC — when the request was resolved. Nullable.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>UserId of the assigned handler. Null when unassigned.</summary>
    public int? AssignedToUserId { get; set; }

    /// <summary>UTC — when this row was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC — when this row was last modified.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>UserId of the creator. Null if the user was deleted.</summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>UserId of the last modifier. Null if the user was deleted.</summary>
    public int? UpdatedByUserId { get; set; }
}