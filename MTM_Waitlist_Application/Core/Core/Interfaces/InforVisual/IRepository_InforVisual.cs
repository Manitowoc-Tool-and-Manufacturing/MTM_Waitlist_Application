using Core.Models.InforVisual;
using Core.Models.Shared;

namespace Core.Interfaces.InforVisual;

/// <summary>
/// Contract for online Infor Visual data access via the backend REST API.
/// </summary>
public interface IRepository_InforVisual
{
    /// <summary>Retrieves all workcenters from the API.</summary>
    Task<Model_Dao_Result<List<Model_VisualWorkcenter>>> GetAllWorkcentersAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves the header details for a single work order from the API.</summary>
    Task<Model_Dao_Result<Model_VisualWorkOrderHeader>> GetWorkOrderHeaderAsync(string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all sequences for a single work order from the API.</summary>
    Task<Model_Dao_Result<List<Model_VisualWorkOrderSequence>>> GetWorkOrderSequencesAsync(string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves subordinate parts for a work order sequence from the API.</summary>
    Task<Model_Dao_Result<List<Model_Visual_SubordinatePart>>> GetSubordinatePartsAsync(string workOrderId, int sequenceNo, CancellationToken cancellationToken = default);

    /// <summary>Retrieves active work orders currently assigned to a workcenter from the API.</summary>
    Task<Model_Dao_Result<List<Model_VisualWorkOrderHeader>>> GetActiveWorkOrdersForResourceAsync(string resourceId, CancellationToken cancellationToken = default);
}