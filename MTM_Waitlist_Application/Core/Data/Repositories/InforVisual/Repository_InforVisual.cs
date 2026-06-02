using Core.Constants.Api;
using Core.Interfaces.Api;
using Core.Interfaces.InforVisual;
using Core.Models.InforVisual;
using Core.Models.Shared;

namespace Data.Repositories.InforVisual;

/// <summary>
/// Online Infor Visual repository that delegates all reads to the backend REST API.
/// </summary>
public sealed class Repository_InforVisual : IRepository_InforVisual
{
    private readonly IApiClient _apiClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository_InforVisual"/> class.
    /// </summary>
    public Repository_InforVisual(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_VisualWorkcenter>>> GetAllWorkcentersAsync(CancellationToken cancellationToken = default)
        => await _apiClient.GetAsync<List<Model_VisualWorkcenter>>(Constants_Api.VisualWorkcenters, cancellationToken);

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<Model_VisualWorkOrderHeader>> GetWorkOrderHeaderAsync(string workOrderId, CancellationToken cancellationToken = default)
        => await _apiClient.GetAsync<Model_VisualWorkOrderHeader>(string.Format(Constants_Api.VisualWorkOrderHeader, workOrderId), cancellationToken);

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_VisualWorkOrderSequence>>> GetWorkOrderSequencesAsync(string workOrderId, CancellationToken cancellationToken = default)
        => await _apiClient.GetAsync<List<Model_VisualWorkOrderSequence>>(string.Format(Constants_Api.VisualWorkOrderSequences, workOrderId), cancellationToken);

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_Visual_SubordinatePart>>> GetSubordinatePartsAsync(string workOrderId, int sequenceNo, CancellationToken cancellationToken = default)
        => await _apiClient.GetAsync<List<Model_Visual_SubordinatePart>>(string.Format(Constants_Api.VisualSubordinateParts, workOrderId, sequenceNo), cancellationToken);

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_VisualWorkOrderHeader>>> GetActiveWorkOrdersForResourceAsync(string resourceId, CancellationToken cancellationToken = default)
        => await _apiClient.GetAsync<List<Model_VisualWorkOrderHeader>>(string.Format(Constants_Api.VisualActiveWorkOrders, resourceId), cancellationToken);
}