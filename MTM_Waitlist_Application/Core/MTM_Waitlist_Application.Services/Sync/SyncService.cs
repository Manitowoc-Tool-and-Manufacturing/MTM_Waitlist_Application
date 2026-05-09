using MTM_Waitlist_Application.Core.Constants.Api;
using MTM_Waitlist_Application.Core.Interfaces.Api;
using MTM_Waitlist_Application.Core.Interfaces.Sync;
using MTM_Waitlist_Application.Core.Interfaces.Waitlist;
using MTM_Waitlist_Application.Core.Models.Shared;
using MTM_Waitlist_Application.Core.Models.Waitlist;

namespace MTM_Waitlist_Application.Services.Sync;

/// <summary>
/// Watches the device's network connectivity and automatically flushes the offline
/// write queue back to the backend API when the work-network connection is restored.
/// The subscription is established in the constructor so it is active before any
/// screen loads — the DI container must resolve this service eagerly at startup
/// (see <c>MauiProgramExtensions.UseSharedMauiApp</c>).
/// </summary>
public sealed class SyncService : ISyncService
{
    private readonly IConnectivity _connectivity;
    private readonly IRepository_WaitlistEntryLocal _localRepository;
    private readonly IApiClient _apiClient;
    private readonly SemaphoreSlim _syncGate = new(1, 1);

    /// <summary>
    /// Initialises a new instance and immediately subscribes to connectivity changes.
    /// </summary>
    public SyncService(
        IConnectivity connectivity,
        IRepository_WaitlistEntryLocal localRepository,
        IApiClient apiClient)
    {
        _connectivity = connectivity;
        _localRepository = localRepository;
        _apiClient = apiClient;

        // Subscribe here — not lazily — so the first reconnect event is never missed.
        _connectivity.ConnectivityChanged += OnConnectivityChangedAsync;
    }

    /// <summary>
    /// Fires when device connectivity changes.
    /// Triggers a queue flush the moment internet access is re-established.
    /// The async-void pattern is required for event handlers, so failures are contained
    /// to prevent unhandled-exception crashes on reconnect.
    /// </summary>
    private async void OnConnectivityChangedAsync(object? sender, ConnectivityChangedEventArgs e)
    {
        try
        {
            if (e.NetworkAccess == NetworkAccess.Internet)
            {
                await FlushOfflineQueueAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Sync failed after connectivity changed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> FlushOfflineQueueAsync(CancellationToken cancellationToken = default)
    {
        if (!await _syncGate.WaitAsync(0, cancellationToken))
        {
            return Model_Dao_Result.Success();
        }

        try
        {
            var pendingResult = await _localRepository.GetPendingWritesAsync();
            if (!pendingResult.IsSuccess)
            {
                return Model_Dao_Result.Failure(pendingResult.ErrorMessage);
            }

            foreach (var (queueId, entry, operation) in pendingResult.Data!)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var syncResult = operation switch
                {
                    "INSERT" => ToVoidResult(await _apiClient.PostAsync<Model_WaitlistEntry>(
                        Constants_Api.WaitlistEntryEndpoint, entry, cancellationToken)),

                    "UPDATE" => ToVoidResult(await _apiClient.PutAsync<Model_WaitlistEntry>(
                        $"{Constants_Api.WaitlistEntryEndpoint}/{entry.Id}", entry, cancellationToken)),

                    "DELETE" => await _apiClient.DeleteAsync(
                        $"{Constants_Api.WaitlistEntryEndpoint}/{entry.Id}", cancellationToken),

                    _ => Model_Dao_Result.Failure($"Unknown queued operation: {operation}")
                };

                if (syncResult.IsSuccess)
                {
                    // Remove from queue only after confirmed API success.
                    await _localRepository.ClearPendingWriteAsync(queueId);
                }
                // On per-item failure we continue — partial sync is better than aborting entirely.
            }

            return Model_Dao_Result.Success();
        }
        finally
        {
            _syncGate.Release();
        }
    }

    /// <summary>
    /// Converts a data-returning result to a void result, preserving the error message.
    /// </summary>
    private static Model_Dao_Result ToVoidResult<T>(Model_Dao_Result<T> result) =>
        result.IsSuccess ? Model_Dao_Result.Success() : Model_Dao_Result.Failure(result.ErrorMessage);
}
