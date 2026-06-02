using Core.Models.SetupTech;
using Core.Models.Shared;

namespace Core.Interfaces.SetupTech;

/// <summary>
/// Contract for offline Setup Tech dunnage type configuration cache access via SQLite.
/// </summary>
public interface IRepository_SetupTechDunnageTypeConfigLocal
{
    /// <summary>Retrieves cached enabled dunnage types from local storage.</summary>
    Task<Model_Dao_Result<List<Model_SetupTech_DunnageTypeConfig>>> GetCachedDunnageTypesAsync();

    /// <summary>Saves the current enabled dunnage type set into local storage.</summary>
    Task<Model_Dao_Result> SaveDunnageTypesAsync(List<Model_SetupTech_DunnageTypeConfig> dunnageTypes);
}