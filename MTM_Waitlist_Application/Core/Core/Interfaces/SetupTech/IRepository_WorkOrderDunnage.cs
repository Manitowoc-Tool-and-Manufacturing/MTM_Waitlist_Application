using Core.Models.SetupTech;
using Core.Models.Shared;

namespace Core.Interfaces.SetupTech;

/// <summary>
/// Contract for online dunnage assignment access via the backend REST API.
/// </summary>
public interface IRepository_WorkOrderDunnage
{
    /// <summary>Retrieves all cached dunnage assignments for a work order sequence from the API.</summary>
    Task<Model_Dao_Result<List<Model_SetupTech_DunnageAssignment>>> GetDunnageAssignmentAsync(string workOrderId, int sequenceNo, CancellationToken cancellationToken = default);

    /// <summary>Inserts or updates a dunnage assignment row through the API.</summary>
    Task<Model_Dao_Result> UpsertDunnageAssignmentAsync(Model_SetupTech_DunnageAssignment assignment, CancellationToken cancellationToken = default);

    /// <summary>Deletes a dunnage assignment row by its identifier through the API.</summary>
    Task<Model_Dao_Result> DeleteDunnageAssignmentAsync(int assignmentId, CancellationToken cancellationToken = default);
}