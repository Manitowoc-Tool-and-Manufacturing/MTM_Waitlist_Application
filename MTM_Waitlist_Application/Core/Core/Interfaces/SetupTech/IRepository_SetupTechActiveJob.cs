using Core.Models.SetupTech;
using Core.Models.Shared;

namespace Core.Interfaces.SetupTech;

/// <summary>
/// Contract for online Setup Tech active-job and history data access via the REST API.
/// </summary>
public interface IRepository_SetupTechActiveJob
{
    /// <summary>Retrieves the current active job for a workstation from the API.</summary>
    Task<Model_Dao_Result<Model_SetupTech_ActiveJob>> GetActiveJobAsync(string workcenterId, CancellationToken cancellationToken = default);

    /// <summary>Saves the current active job for a workstation through the API.</summary>
    Task<Model_Dao_Result> SetActiveJobAsync(Model_SetupTech_ActiveJob activeJob, CancellationToken cancellationToken = default);

    /// <summary>Retrieves archived job history for a workstation from the API.</summary>
    Task<Model_Dao_Result<List<Model_SetupTech_JobHistoryEntry>>> GetJobHistoryAsync(string workcenterId, CancellationToken cancellationToken = default);
}