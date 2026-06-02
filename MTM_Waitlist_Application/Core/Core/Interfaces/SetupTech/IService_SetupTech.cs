using Core.Models.SetupTech;
using Core.Models.Shared;

namespace Core.Interfaces.SetupTech;

/// <summary>
/// Business logic contract for Setup Tech workstation job configuration operations.
/// </summary>
public interface IService_SetupTech
{
    /// <summary>Returns the current active job for a workstation.</summary>
    Task<Model_Dao_Result<Model_SetupTech_ActiveJob>> GetActiveJobAsync(string workcenterId, CancellationToken cancellationToken = default);

    /// <summary>Saves the current active job for a workstation.</summary>
    Task<Model_Dao_Result> SetActiveJobAsync(Model_SetupTech_ActiveJob activeJob, CancellationToken cancellationToken = default);

    /// <summary>Returns cached dunnage assignments for a work order sequence.</summary>
    Task<Model_Dao_Result<List<Model_SetupTech_DunnageAssignment>>> GetDunnageAssignmentAsync(string workOrderId, int sequenceNo, CancellationToken cancellationToken = default);

    /// <summary>Inserts or updates a dunnage assignment for a work order sequence.</summary>
    Task<Model_Dao_Result> UpsertDunnageAssignmentAsync(Model_SetupTech_DunnageAssignment assignment, CancellationToken cancellationToken = default);

    /// <summary>Deletes a dunnage assignment by its identifier.</summary>
    Task<Model_Dao_Result> DeleteDunnageAssignmentAsync(int assignmentId, CancellationToken cancellationToken = default);

    /// <summary>Returns all enabled dunnage types for the Setup Tech picker UI.</summary>
    Task<Model_Dao_Result<List<Model_SetupTech_DunnageTypeConfig>>> GetEnabledDunnageTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns all dunnage catalog items available for assignment.</summary>
    Task<Model_Dao_Result<List<Model_DunnagePart>>> GetDunnageCatalogAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns archived job history for a workstation.</summary>
    Task<Model_Dao_Result<List<Model_SetupTech_JobHistoryEntry>>> GetJobHistoryAsync(string workcenterId, CancellationToken cancellationToken = default);
}