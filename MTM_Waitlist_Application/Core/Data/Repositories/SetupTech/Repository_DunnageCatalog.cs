using Core.Constants.Api;
using Core.Interfaces.Api;
using Core.Interfaces.SetupTech;
using Core.Models.SetupTech;
using Core.Models.Shared;

namespace Data.Repositories.SetupTech;

/// <summary>
/// Online repository for retrieving dunnage catalog items used by the Setup Tech picker UI.
/// </summary>
public sealed class Repository_DunnageCatalog : IRepository_DunnageCatalog
{
    private readonly IApiClient _apiClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository_DunnageCatalog"/> class.
    /// </summary>
    public Repository_DunnageCatalog(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_DunnagePart>>> GetDunnageCatalogAsync(CancellationToken cancellationToken = default)
        => await _apiClient.GetAsync<List<Model_DunnagePart>>(Constants_Api.DunnageCatalog, cancellationToken);
}