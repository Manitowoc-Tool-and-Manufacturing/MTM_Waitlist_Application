using Core.Models.Shared;
using Core.Models.Waitlist;

namespace Core.Interfaces.Waitlist;

/// <summary>
/// Contract for offline waitlist data access via the on-device SQLite cache.
/// Implementations delegate exclusively to the local database —
/// no connectivity checks or API calls belong here.
/// Also manages the offline write queue used by <see cref="Sync.ISyncService"/>
/// to replay local changes once connectivity is restored.
/// </summary>
public interface IRepository_WaitlistEntryLocal
{
    /// <summary>Retrieves all cached waitlist entries from local storage.</summary>
    Task<Model_Dao_Result<List<Model_WaitlistEntry>>> GetAllWaitlistEntriesAsync();

    /// <summary>Retrieves a single cached waitlist entry by its <paramref name="id"/>.</summary>
    Task<Model_Dao_Result<Model_WaitlistEntry>> GetWaitlistEntryByIdAsync(int id);

    /// <summary>Inserts a new waitlist entry into the local cache.</summary>
    Task<Model_Dao_Result> InsertWaitlistEntryAsync(Model_WaitlistEntry entry);

    /// <summary>Updates an existing waitlist entry in the local cache.</summary>
    Task<Model_Dao_Result> UpdateWaitlistEntryAsync(Model_WaitlistEntry entry);

    /// <summary>Deletes a waitlist entry from the local cache by <paramref name="id"/>.</summary>
    Task<Model_Dao_Result> DeleteWaitlistEntryAsync(int id);

    /// <summary>
    /// Adds a pending write to the offline queue so it can be replayed
    /// against the API once connectivity is restored.
    /// </summary>
    /// <param name="entry">The entry involved in the queued write.</param>
    /// <param name="operation">One of "INSERT", "UPDATE", or "DELETE".</param>
    Task<Model_Dao_Result> EnqueuePendingWriteAsync(Model_WaitlistEntry entry, string operation);

    /// <summary>Returns all pending writes that have not yet been synced to the API.</summary>
    Task<Model_Dao_Result<List<(int QueueId, Model_WaitlistEntry Entry, string Operation)>>> GetPendingWritesAsync();

    /// <summary>Removes a successfully synced item from the offline queue by its <paramref name="queueId"/>.</summary>
    Task<Model_Dao_Result> ClearPendingWriteAsync(int queueId);
}
