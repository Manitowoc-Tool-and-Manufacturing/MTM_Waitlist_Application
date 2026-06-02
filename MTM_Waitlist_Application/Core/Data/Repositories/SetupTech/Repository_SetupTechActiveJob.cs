using Core.Constants.Api;
using Core.Interfaces.Api;
using Core.Interfaces.SetupTech;
using Core.Models.SetupTech;
using Core.Models.Shared;

namespace Data.Repositories.SetupTech;

/// <summary>
/// Online Setup Tech repository for active-job and job-history reads through the backend REST API.
/// </summary>
public sealed class Repository_SetupTechActiveJob : IRepository_SetupTechActiveJob
{
    private readonly IApiClient _apiClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository_SetupTechActiveJob"/> class.
    /// </summary>
    public Repository_SetupTechActiveJob(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<Model_SetupTech_ActiveJob>> GetActiveJobAsync(string workcenterId, CancellationToken cancellationToken = default)
        => await _apiClient.GetAsync<Model_SetupTech_ActiveJob>(string.Format(Constants_Api.SetupTechActiveJob, workcenterId), cancellationToken);

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_SetupTech_JobHistoryEntry>>> GetJobHistoryAsync(string workcenterId, CancellationToken cancellationToken = default)
        => await _apiClient.GetAsync<List<Model_SetupTech_JobHistoryEntry>>(string.Format(Constants_Api.SetupTechJobHistory, workcenterId), cancellationToken);

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> SetActiveJobAsync(Model_SetupTech_ActiveJob activeJob, CancellationToken cancellationToken = default)
    {
        var result = await _apiClient.PostAsync<Model_SetupTech_ActiveJob>(Constants_Api.SetupTechSetActiveJob, activeJob, cancellationToken);
        return result.IsSuccess
            ? Model_Dao_Result.Success()
            : Model_Dao_Result.Failure(result.ErrorMessage);
    }
}