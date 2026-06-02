using Core.Constants.Api;
using Core.Interfaces.Api;
using Core.Interfaces.SetupTech;
using Core.Models.SetupTech;
using Core.Models.Shared;

namespace Data.Repositories.SetupTech;

/// <summary>
/// Online Setup Tech repository for enabled dunnage type configuration.
/// </summary>
public sealed class Repository_SetupTechDunnageTypeConfig : IRepository_SetupTechDunnageTypeConfig
{
    private readonly IApiClient _apiClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository_SetupTechDunnageTypeConfig"/> class.
    /// </summary>
    public Repository_SetupTechDunnageTypeConfig(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_SetupTech_DunnageTypeConfig>>> GetEnabledDunnageTypesAsync(CancellationToken cancellationToken = default)
        => await _apiClient.GetAsync<List<Model_SetupTech_DunnageTypeConfig>>(Constants_Api.SetupTechDunnageTypes, cancellationToken);
}