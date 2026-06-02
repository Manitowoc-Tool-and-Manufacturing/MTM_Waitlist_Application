using Core.Models.SetupTech;
using Core.Models.Shared;

namespace Core.Interfaces.SetupTech;

/// <summary>
/// Contract for offline Setup Tech active-job and history cache access via SQLite.
/// </summary>
public interface IRepository_SetupTechActiveJobLocal
{
    /// <summary>Retrieves the cached active job for a workstation from local storage.</summary>
    Task<Model_Dao_Result<Model_SetupTech_ActiveJob>> GetActiveJobAsync(string workcenterId);

    /// <summary>Saves the active job cache for a workstation in local storage.</summary>
    Task<Model_Dao_Result> SetActiveJobAsync(Model_SetupTech_ActiveJob activeJob);

    /// <summary>Retrieves cached job history for a workstation from local storage.</summary>
    Task<Model_Dao_Result<List<Model_SetupTech_JobHistoryEntry>>> GetJobHistoryAsync(string workcenterId);
}