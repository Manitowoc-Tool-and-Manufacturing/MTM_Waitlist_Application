using Core.Interfaces.Waitlist;
using Core.Models.Shared;
using Core.Models.Waitlist;

namespace Services.Waitlist;

/// <summary>
/// Business logic layer for waitlist entry operations.
/// Routes each request to the online repository (API) when the device has internet
/// access, and falls back silently to the local SQLite cache when offline or when a
/// mid-request network failure is detected.
/// Write operations performed offline are queued in the local repository so the
/// sync service can replay them against the API when connectivity is restored.
/// </summary>
public sealed class Service_WaitlistEntry : IService_WaitlistEntry
{
    private readonly IConnectivity _connectivity;
    private readonly IRepository_WaitlistEntry _onlineRepository;
    private readonly IRepository_WaitlistEntryLocal _localRepository;

    /// <summary>
    /// Initialises a new instance with the required connectivity provider and repositories.
    /// </summary>
    public Service_WaitlistEntry(
        IConnectivity connectivity,
        IRepository_WaitlistEntry onlineRepository,
        IRepository_WaitlistEntryLocal localRepository)
    {
        _connectivity = connectivity;
        _onlineRepository = onlineRepository;
        _localRepository = localRepository;
    }

    /// <summary>
    /// Returns <see langword="true"/> when the device currently reports internet access.
    /// </summary>
    private bool IsOnline => _connectivity.NetworkAccess == NetworkAccess.Internet;

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_WaitlistEntry>>> GetAllEntriesAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsOnline)
        {
            var onlineResult = await _onlineRepository.GetAllWaitlistEntriesAsync(cancellationToken);
            if (onlineResult.IsSuccess)
            {
                return onlineResult;
            }
            // Mid-request network failure — fall through to local cache silently.
        }

        return await _localRepository.GetAllWaitlistEntriesAsync();
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<Model_WaitlistEntry>> GetEntryByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        if (IsOnline)
        {
            var onlineResult = await _onlineRepository.GetWaitlistEntryByIdAsync(id, cancellationToken);
            if (onlineResult.IsSuccess)
            {
                return onlineResult;
            }
            // Mid-request network failure — fall through to local cache silently.
        }

        return await _localRepository.GetWaitlistEntryByIdAsync(id);
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> SaveEntryAsync(
        Model_WaitlistEntry entry, CancellationToken cancellationToken = default)
    {
        if (IsOnline)
        {
            var onlineResult = await _onlineRepository.InsertWaitlistEntryAsync(entry, cancellationToken);
            if (onlineResult.IsSuccess)
            {
                // Mirror to local cache so offline reads stay current after an online write.
                await _localRepository.InsertWaitlistEntryAsync(entry);
                return onlineResult;
            }
            // Mid-request network failure — fall through to offline path.
        }

        // Save locally and enqueue for replay once connectivity is restored.
        var localResult = await _localRepository.InsertWaitlistEntryAsync(entry);
        if (localResult.IsSuccess)
        {
            await _localRepository.EnqueuePendingWriteAsync(entry, "INSERT");
        }

        return localResult;
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> UpdateEntryAsync(
        Model_WaitlistEntry entry, CancellationToken cancellationToken = default)
    {
        if (IsOnline)
        {
            var onlineResult = await _onlineRepository.UpdateWaitlistEntryAsync(entry, cancellationToken);
            if (onlineResult.IsSuccess)
            {
                await _localRepository.UpdateWaitlistEntryAsync(entry);
                return onlineResult;
            }
        }

        var localResult = await _localRepository.UpdateWaitlistEntryAsync(entry);
        if (localResult.IsSuccess)
        {
            await _localRepository.EnqueuePendingWriteAsync(entry, "UPDATE");
        }

        return localResult;
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> DeleteEntryAsync(
        int id, CancellationToken cancellationToken = default)
    {
        if (IsOnline)
        {
            var onlineResult = await _onlineRepository.DeleteWaitlistEntryAsync(id, cancellationToken);
            if (onlineResult.IsSuccess)
            {
                await _localRepository.DeleteWaitlistEntryAsync(id);
                return onlineResult;
            }
        }

        // Create a placeholder entry carrying only the id so the queue has enough
        // information to issue a DELETE against the correct API endpoint on sync.
        var placeholder = new Model_WaitlistEntry { Id = id };
        var localResult = await _localRepository.DeleteWaitlistEntryAsync(id);
        if (localResult.IsSuccess)
        {
            await _localRepository.EnqueuePendingWriteAsync(placeholder, "DELETE");
        }

        return localResult;
    }
}
