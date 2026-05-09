using MTM_Waitlist_Application.Core.Constants.Api;
using MTM_Waitlist_Application.Core.Interfaces.Waitlist;
using MTM_Waitlist_Application.Core.Models.Shared;
using MTM_Waitlist_Application.Core.Models.Waitlist;
using MTM_Waitlist_Application.Core.Interfaces.Api;

namespace MTM_Waitlist_Application.Data.Repositories.Waitlist;

/// <summary>
/// Online implementation of <see cref="IRepository_WaitlistEntry"/> that delegates
/// all data access to the backend REST API via <see cref="IApiClient"/>.
/// No connectivity checks are performed here — that responsibility belongs
/// to the Service layer.  All API exceptions are caught by <see cref="IApiClient"/>
/// and returned as <see cref="Model_Dao_Result"/> failures.
/// </summary>
public sealed class Repository_WaitlistEntry : IRepository_WaitlistEntry
{
    private readonly IApiClient _apiClient;

    /// <summary>
    /// Initialises a new instance with the supplied API client.
    /// </summary>
    public Repository_WaitlistEntry(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_WaitlistEntry>>> GetAllWaitlistEntriesAsync(CancellationToken cancellationToken = default)
        => await _apiClient.GetAsync<List<Model_WaitlistEntry>>(Constants_Api.WaitlistEntryEndpoint, cancellationToken);

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<Model_WaitlistEntry>> GetWaitlistEntryByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _apiClient.GetAsync<Model_WaitlistEntry>($"{Constants_Api.WaitlistEntryEndpoint}/{id}", cancellationToken);

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> InsertWaitlistEntryAsync(Model_WaitlistEntry entry, CancellationToken cancellationToken = default)
    {
        var result = await _apiClient.PostAsync<Model_WaitlistEntry>(Constants_Api.WaitlistEntryEndpoint, entry, cancellationToken);
        return result.IsSuccess
            ? Model_Dao_Result.Success()
            : Model_Dao_Result.Failure(result.ErrorMessage);
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> UpdateWaitlistEntryAsync(Model_WaitlistEntry entry, CancellationToken cancellationToken = default)
    {
        var result = await _apiClient.PutAsync<Model_WaitlistEntry>($"{Constants_Api.WaitlistEntryEndpoint}/{entry.Id}", entry, cancellationToken);
        return result.IsSuccess
            ? Model_Dao_Result.Success()
            : Model_Dao_Result.Failure(result.ErrorMessage);
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> DeleteWaitlistEntryAsync(int id, CancellationToken cancellationToken = default)
        => await _apiClient.DeleteAsync($"{Constants_Api.WaitlistEntryEndpoint}/{id}", cancellationToken);
}
