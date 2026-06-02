using Core.Models.SetupTech;
using Core.Models.Shared;

namespace Core.Interfaces.SetupTech;

/// <summary>
/// Contract for offline dunnage assignment cache access via SQLite.
/// </summary>
public interface IRepository_WorkOrderDunnageLocal
{
    /// <summary>Retrieves cached dunnage assignments for a work order sequence from local storage.</summary>
    Task<Model_Dao_Result<List<Model_SetupTech_DunnageAssignment>>> GetDunnageAssignmentAsync(string workOrderId, int sequenceNo);

    /// <summary>Inserts or updates a dunnage assignment row in local storage.</summary>
    Task<Model_Dao_Result> UpsertDunnageAssignmentAsync(Model_SetupTech_DunnageAssignment assignment);

    /// <summary>Deletes a dunnage assignment row by its identifier from local storage.</summary>
    Task<Model_Dao_Result> DeleteDunnageAssignmentAsync(int assignmentId);
}