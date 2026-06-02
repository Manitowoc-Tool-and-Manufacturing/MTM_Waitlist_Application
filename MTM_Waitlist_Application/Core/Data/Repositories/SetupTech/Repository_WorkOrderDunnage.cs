using Core.Constants.Api;
using Core.Interfaces.Api;
using Core.Interfaces.SetupTech;
using Core.Models.SetupTech;
using Core.Models.Shared;

namespace Data.Repositories.SetupTech;

/// <summary>
/// Online Setup Tech repository for work-order dunnage assignments.
/// </summary>
public sealed class Repository_WorkOrderDunnage : IRepository_WorkOrderDunnage
{
    private readonly IApiClient _apiClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository_WorkOrderDunnage"/> class.
    /// </summary>
    public Repository_WorkOrderDunnage(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> DeleteDunnageAssignmentAsync(int assignmentId, CancellationToken cancellationToken = default)
        => await _apiClient.DeleteAsync(string.Format(Constants_Api.SetupTechDeleteDunnage, assignmentId), cancellationToken);

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_SetupTech_DunnageAssignment>>> GetDunnageAssignmentAsync(string workOrderId, int sequenceNo, CancellationToken cancellationToken = default)
        => await _apiClient.GetAsync<List<Model_SetupTech_DunnageAssignment>>(string.Format(Constants_Api.SetupTechDunnageAssignment, workOrderId, sequenceNo), cancellationToken);

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> UpsertDunnageAssignmentAsync(Model_SetupTech_DunnageAssignment assignment, CancellationToken cancellationToken = default)
    {
        var result = await _apiClient.PutAsync<Model_SetupTech_DunnageAssignment>(Constants_Api.SetupTechUpsertDunnage, assignment, cancellationToken);
        return result.IsSuccess
            ? Model_Dao_Result.Success()
            : Model_Dao_Result.Failure(result.ErrorMessage);
    }
}