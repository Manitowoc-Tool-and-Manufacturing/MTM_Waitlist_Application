using MTM_Waitlist_Application.Core.Models.Shared;
using MTM_Waitlist_Application.Core.Models.Waitlist;

namespace MTM_Waitlist_Application.Core.Interfaces.Waitlist;

/// <summary>
/// Business logic contract for waitlist entry operations.
/// Implementations are responsible for connectivity-aware routing between
/// the online repository (API) and the offline local cache,
/// and for queuing offline writes for later sync.
/// ViewModels must only interact with this interface — never with repositories directly.
/// </summary>
public interface IService_WaitlistEntry
{
    /// <summary>
    /// Returns all waitlist entries. Uses the API when online;
    /// falls back to local cache when offline or on network failure.
    /// </summary>
    Task<Model_Dao_Result<List<Model_WaitlistEntry>>> GetAllEntriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single waitlist entry by <paramref name="id"/>.
    /// Falls back to local cache when offline or on network failure.
    /// </summary>
    Task<Model_Dao_Result<Model_WaitlistEntry>> GetEntryByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a new waitlist entry. Sends to the API when online;
    /// stores locally and queues for sync when offline.
    /// </summary>
    Task<Model_Dao_Result> SaveEntryAsync(Model_WaitlistEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing waitlist entry. Sends to the API when online;
    /// updates locally and queues for sync when offline.
    /// </summary>
    Task<Model_Dao_Result> UpdateEntryAsync(Model_WaitlistEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a waitlist entry by <paramref name="id"/>. Sends to the API when online;
    /// deletes locally and queues for sync when offline.
    /// </summary>
    Task<Model_Dao_Result> DeleteEntryAsync(int id, CancellationToken cancellationToken = default);
}
