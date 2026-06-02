using Core.Models.InforVisual;
using Core.Models.Shared;

namespace Core.Interfaces.InforVisual;

/// <summary>
/// Contract for offline Infor Visual cache access in the local SQLite store.
/// Only workcenters are cached locally.
/// </summary>
public interface IRepository_InforVisualLocal
{
    /// <summary>Retrieves cached workcenters from local storage.</summary>
    Task<Model_Dao_Result<List<Model_VisualWorkcenter>>> GetCachedWorkcentersAsync();

    /// <summary>Saves a fresh set of workcenters to the local cache.</summary>
    Task<Model_Dao_Result> SaveWorkcentersAsync(List<Model_VisualWorkcenter> workcenters);
}