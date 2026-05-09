using SQLite;

namespace MTM_Waitlist_Application.Data.Local;

/// <summary>
/// SQLite-mapped entity that records a pending write for the offline sync queue.
/// Each row represents a single create, update, or delete that was made while
/// the device was offline and has not yet been replayed against the API.
/// </summary>
[Table("OfflineWriteQueue")]
internal sealed class Entity_OfflineWriteQueue
{
    /// <summary>Auto-incremented surrogate key used to identify and remove queue items after sync.</summary>
    [PrimaryKey, AutoIncrement]
    public int QueueId { get; set; }

    /// <summary>The waitlist entry ID affected by this queued write.</summary>
    public int EntryId { get; set; }

    /// <summary>Serialised JSON snapshot of the entry at the time of the queued write.</summary>
    public string EntryJson { get; set; } = string.Empty;

    /// <summary>One of "INSERT", "UPDATE", or "DELETE".</summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>UTC timestamp when this item was enqueued.</summary>
    public DateTimeOffset EnqueuedAt { get; set; }
}
