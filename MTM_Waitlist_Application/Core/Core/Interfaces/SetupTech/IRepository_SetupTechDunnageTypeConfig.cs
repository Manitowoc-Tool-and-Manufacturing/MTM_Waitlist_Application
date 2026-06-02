using Core.Models.SetupTech;
using Core.Models.Shared;

namespace Core.Interfaces.SetupTech;

/// <summary>
/// Contract for online Setup Tech dunnage type configuration reads via the REST API.
/// </summary>
public interface IRepository_SetupTechDunnageTypeConfig
{
    /// <summary>Retrieves all enabled Setup Tech dunnage types from the API.</summary>
    Task<Model_Dao_Result<List<Model_SetupTech_DunnageTypeConfig>>> GetEnabledDunnageTypesAsync(CancellationToken cancellationToken = default);
}