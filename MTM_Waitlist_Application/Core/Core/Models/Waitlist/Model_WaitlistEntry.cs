using Core.Enums.Waitlist;

namespace Core.Models.Waitlist;

/// <summary>
/// Represents a single workcenter logistics/material-handling request on the waitlist.
/// A workcenter submits a request (e.g., deliver a coil, pick up finished goods) which
/// is queued, assigned to a material handler, and tracked through to completion.
/// </summary>
public sealed class Model_WaitlistEntry
{
    /// <summary>Unique identifier for the waitlist entry.</summary>
    public int Id { get; set; }

    /// <summary>
    /// The name of the workcenter submitting this request (e.g., "Press 3", "Line 7").
    /// </summary>
    public string WorkcenterName { get; set; } = string.Empty;

    /// <summary>
    /// The category of logistics request.
    /// Determines what action a material handler or setup tech must take.
    /// </summary>
    public Enum_WaitlistRequestType RequestType { get; set; }

    /// <summary>
    /// Current lifecycle state of the request.
    /// Defaults to <see cref="Enum_WaitlistStatus.Waiting"/> on creation.
    /// </summary>
    public Enum_WaitlistStatus Status { get; set; } = Enum_WaitlistStatus.Waiting;

    /// <summary>
    /// Sort priority. 1 = highest urgency, 10 = lowest. Default is 5 (normal).
    /// </summary>
    public int Priority { get; set; } = 5;

    /// <summary>Optional free-text remarks visible to supervisors and material handlers.</summary>
    public string? Notes { get; set; }

    /// <summary>UTC timestamp when this request was submitted to the waitlist.</summary>
    public DateTime RequestedAt { get; set; }

    /// <summary>UTC timestamp of the estimated or confirmed fulfillment time. Nullable.</summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// UTC timestamp when the request was resolved (Completed or Cancelled).
    /// Set automatically by the database trigger on status transition.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// The Id of the user currently assigned to fulfill this request.
    /// <see langword="null"/> when unassigned.
    /// </summary>
    public int? AssignedToUserId { get; set; }

    /// <summary>UTC timestamp when this record was created. Set by the database trigger.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp when this record was last modified. Set by the database trigger.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>The Id of the user who created this record. Null if the user was deleted.</summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>The Id of the user who last modified this record. Null if the user was deleted.</summary>
    public int? UpdatedByUserId { get; set; }
}