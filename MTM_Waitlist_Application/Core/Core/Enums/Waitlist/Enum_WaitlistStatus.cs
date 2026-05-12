namespace Core.Enums.Waitlist;

/// <summary>
/// Lifecycle states for a waitlist entry.
/// Maps to the <c>Status</c> ENUM column in the <c>WaitlistEntries</c> MySQL table.
/// </summary>
public enum Enum_WaitlistStatus
{
    /// <summary>Request is in the queue, awaiting assignment.</summary>
    Waiting,

    /// <summary>Request is currently being handled by an assigned user.</summary>
    Active,

    /// <summary>Request has exceeded its expected completion time.</summary>
    Late,

    /// <summary>Request has been deprioritised and will be handled when capacity allows.</summary>
    LowImportance,

    /// <summary>Request is planned work scheduled for a future date.</summary>
    Project,

    /// <summary>Request was fulfilled successfully.</summary>
    Completed,

    /// <summary>Request was withdrawn or will not be fulfilled.</summary>
    Cancelled,
}
