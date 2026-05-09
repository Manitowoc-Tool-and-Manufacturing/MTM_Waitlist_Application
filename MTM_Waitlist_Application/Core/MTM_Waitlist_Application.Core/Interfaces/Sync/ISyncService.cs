using MTM_Waitlist_Application.Core.Models.Shared;

namespace MTM_Waitlist_Application.Core.Interfaces.Sync;

/// <summary>
/// Contract for flushing the offline write queue back to the backend API
/// after connectivity is restored.
/// Implementations subscribe to <c>IConnectivity.ConnectivityChanged</c>
/// in their constructor so the subscription is active before any screen loads.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Sends all pending offline writes through the API in queue order
    /// and removes each entry from the queue on success.
    /// </summary>
    Task<Model_Dao_Result> FlushOfflineQueueAsync(CancellationToken cancellationToken = default);
}
