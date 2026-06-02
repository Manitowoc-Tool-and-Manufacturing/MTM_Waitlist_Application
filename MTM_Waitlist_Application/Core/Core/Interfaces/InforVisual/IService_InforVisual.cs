using Core.Models.InforVisual;
using Core.Models.Shared;

namespace Core.Interfaces.InforVisual;

/// <summary>
/// Business logic contract for Infor Visual read operations.
/// Implementations are responsible for workcenter caching and offline behavior.
/// </summary>
public interface IService_InforVisual
{
    /// <summary>Returns all available workcenters, using cache-aware routing where applicable.</summary>
    Task<Model_Dao_Result<List<Model_VisualWorkcenter>>> GetAllWorkcentersAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the header details for a single work order.</summary>
    Task<Model_Dao_Result<Model_VisualWorkOrderHeader>> GetWorkOrderHeaderAsync(string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>Returns all available sequences for the supplied work order.</summary>
    Task<Model_Dao_Result<List<Model_VisualWorkOrderSequence>>> GetWorkOrderSequencesAsync(string workOrderId, CancellationToken cancellationToken = default);

    /// <summary>Returns subordinate or component parts for a work order sequence.</summary>
    Task<Model_Dao_Result<List<Model_Visual_SubordinatePart>>> GetSubordinatePartsAsync(string workOrderId, int sequenceNo, CancellationToken cancellationToken = default);

    /// <summary>Returns active work orders currently associated with a workcenter.</summary>
    Task<Model_Dao_Result<List<Model_VisualWorkOrderHeader>>> GetActiveWorkOrdersForResourceAsync(string resourceId, CancellationToken cancellationToken = default);

    /// <summary>Clears the in-memory workcenter cache so the next request refreshes it.</summary>
    void InvalidateWorkcenterCache();
}