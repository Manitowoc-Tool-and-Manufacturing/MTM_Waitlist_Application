using Core.Interfaces.InforVisual;
using Core.Models.InforVisual;
using Core.Models.Shared;

namespace Services.InforVisual;

/// <summary>
/// Connectivity-aware business service for Infor Visual lookups.
/// </summary>
public sealed class Service_InforVisual : IService_InforVisual
{
    private readonly IConnectivity _connectivity;
    private readonly IRepository_InforVisual _onlineRepository;
    private readonly IRepository_InforVisualLocal _localRepository;
    private List<Model_VisualWorkcenter>? _workcenterCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="Service_InforVisual"/> class.
    /// </summary>
    public Service_InforVisual(
        IConnectivity connectivity,
        IRepository_InforVisual onlineRepository,
        IRepository_InforVisualLocal localRepository)
    {
        _connectivity = connectivity;
        _onlineRepository = onlineRepository;
        _localRepository = localRepository;
    }

    private bool IsOnline => _connectivity.NetworkAccess == NetworkAccess.Internet;

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_VisualWorkcenter>>> GetAllWorkcentersAsync(CancellationToken cancellationToken = default)
    {
        if (_workcenterCache is not null)
        {
            return Model_Dao_Result<List<Model_VisualWorkcenter>>.Success([.. _workcenterCache]);
        }

        if (IsOnline)
        {
            var onlineResult = await _onlineRepository.GetAllWorkcentersAsync(cancellationToken);
            if (onlineResult.IsSuccess && onlineResult.Data is not null)
            {
                _workcenterCache = onlineResult.Data;
                await _localRepository.SaveWorkcentersAsync(onlineResult.Data);
                return Model_Dao_Result<List<Model_VisualWorkcenter>>.Success([.. onlineResult.Data]);
            }
        }

        var localResult = await _localRepository.GetCachedWorkcentersAsync();
        if (localResult.IsSuccess && localResult.Data is not null)
        {
            _workcenterCache = localResult.Data;
            return Model_Dao_Result<List<Model_VisualWorkcenter>>.Success([.. localResult.Data]);
        }

        return localResult;
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_VisualWorkOrderHeader>>> GetActiveWorkOrdersForResourceAsync(string resourceId, CancellationToken cancellationToken = default)
    {
        if (!IsOnline)
        {
            return Model_Dao_Result<List<Model_VisualWorkOrderHeader>>.Failure("Infor Visual is not available while offline.");
        }

        return await _onlineRepository.GetActiveWorkOrdersForResourceAsync(resourceId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<Model_VisualWorkOrderHeader>> GetWorkOrderHeaderAsync(string workOrderId, CancellationToken cancellationToken = default)
    {
        if (!IsOnline)
        {
            return Model_Dao_Result<Model_VisualWorkOrderHeader>.Failure("Infor Visual is not available while offline.");
        }

        return await _onlineRepository.GetWorkOrderHeaderAsync(workOrderId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_VisualWorkOrderSequence>>> GetWorkOrderSequencesAsync(string workOrderId, CancellationToken cancellationToken = default)
    {
        if (!IsOnline)
        {
            return Model_Dao_Result<List<Model_VisualWorkOrderSequence>>.Failure("Infor Visual is not available while offline.");
        }

        return await _onlineRepository.GetWorkOrderSequencesAsync(workOrderId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_Visual_SubordinatePart>>> GetSubordinatePartsAsync(string workOrderId, int sequenceNo, CancellationToken cancellationToken = default)
    {
        if (!IsOnline)
        {
            return Model_Dao_Result<List<Model_Visual_SubordinatePart>>.Failure("Infor Visual is not available while offline.");
        }

        return await _onlineRepository.GetSubordinatePartsAsync(workOrderId, sequenceNo, cancellationToken);
    }

    /// <inheritdoc/>
    public void InvalidateWorkcenterCache()
    {
        _workcenterCache = null;
    }
}