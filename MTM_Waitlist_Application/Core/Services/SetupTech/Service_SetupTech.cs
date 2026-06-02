using Core.Interfaces.SetupTech;
using Core.Models.SetupTech;
using Core.Models.Shared;

namespace Services.SetupTech;

/// <summary>
/// Connectivity-aware business service for Setup Tech active-job and dunnage workflows.
/// </summary>
public sealed class Service_SetupTech : IService_SetupTech
{
    private readonly IConnectivity _connectivity;
    private readonly IRepository_SetupTechActiveJob _activeJobRepository;
    private readonly IRepository_SetupTechActiveJobLocal _activeJobLocalRepository;
    private readonly IRepository_DunnageCatalog _dunnageCatalogRepository;
    private readonly IRepository_WorkOrderDunnage _dunnageRepository;
    private readonly IRepository_WorkOrderDunnageLocal _dunnageLocalRepository;
    private readonly IRepository_SetupTechDunnageTypeConfig _dunnageTypeRepository;
    private readonly IRepository_SetupTechDunnageTypeConfigLocal _dunnageTypeLocalRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="Service_SetupTech"/> class.
    /// </summary>
    public Service_SetupTech(
        IConnectivity connectivity,
        IRepository_SetupTechActiveJob activeJobRepository,
        IRepository_SetupTechActiveJobLocal activeJobLocalRepository,
        IRepository_DunnageCatalog dunnageCatalogRepository,
        IRepository_WorkOrderDunnage dunnageRepository,
        IRepository_WorkOrderDunnageLocal dunnageLocalRepository,
        IRepository_SetupTechDunnageTypeConfig dunnageTypeRepository,
        IRepository_SetupTechDunnageTypeConfigLocal dunnageTypeLocalRepository)
    {
        _connectivity = connectivity;
        _activeJobRepository = activeJobRepository;
        _activeJobLocalRepository = activeJobLocalRepository;
        _dunnageCatalogRepository = dunnageCatalogRepository;
        _dunnageRepository = dunnageRepository;
        _dunnageLocalRepository = dunnageLocalRepository;
        _dunnageTypeRepository = dunnageTypeRepository;
        _dunnageTypeLocalRepository = dunnageTypeLocalRepository;
    }

    private bool IsOnline => _connectivity.NetworkAccess == NetworkAccess.Internet;

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> DeleteDunnageAssignmentAsync(int assignmentId, CancellationToken cancellationToken = default)
    {
        if (!IsOnline)
        {
            return Model_Dao_Result.Failure("Dunnage assignments cannot be changed while offline.");
        }

        var result = await _dunnageRepository.DeleteDunnageAssignmentAsync(assignmentId, cancellationToken);
        if (result.IsSuccess)
        {
            await _dunnageLocalRepository.DeleteDunnageAssignmentAsync(assignmentId);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<Model_SetupTech_ActiveJob>> GetActiveJobAsync(string workcenterId, CancellationToken cancellationToken = default)
    {
        if (IsOnline)
        {
            var onlineResult = await _activeJobRepository.GetActiveJobAsync(workcenterId, cancellationToken);
            if (onlineResult.IsSuccess && onlineResult.Data is not null)
            {
                await _activeJobLocalRepository.SetActiveJobAsync(onlineResult.Data);
                return onlineResult;
            }
        }

        return await _activeJobLocalRepository.GetActiveJobAsync(workcenterId);
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_SetupTech_DunnageAssignment>>> GetDunnageAssignmentAsync(string workOrderId, int sequenceNo, CancellationToken cancellationToken = default)
    {
        if (IsOnline)
        {
            var onlineResult = await _dunnageRepository.GetDunnageAssignmentAsync(workOrderId, sequenceNo, cancellationToken);
            if (onlineResult.IsSuccess && onlineResult.Data is not null)
            {
                var localResult = await _dunnageLocalRepository.GetDunnageAssignmentAsync(workOrderId, sequenceNo);
                if (localResult.IsSuccess && localResult.Data is not null)
                {
                    var onlineIds = onlineResult.Data.Select(assignment => assignment.AssignmentId).ToHashSet();
                    foreach (var cachedAssignment in localResult.Data.Where(assignment => !onlineIds.Contains(assignment.AssignmentId)))
                    {
                        await _dunnageLocalRepository.DeleteDunnageAssignmentAsync(cachedAssignment.AssignmentId);
                    }
                }

                foreach (var assignment in onlineResult.Data)
                {
                    await _dunnageLocalRepository.UpsertDunnageAssignmentAsync(assignment);
                }

                return onlineResult;
            }
        }

        return await _dunnageLocalRepository.GetDunnageAssignmentAsync(workOrderId, sequenceNo);
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_SetupTech_DunnageTypeConfig>>> GetEnabledDunnageTypesAsync(CancellationToken cancellationToken = default)
    {
        if (IsOnline)
        {
            var onlineResult = await _dunnageTypeRepository.GetEnabledDunnageTypesAsync(cancellationToken);
            if (onlineResult.IsSuccess && onlineResult.Data is not null)
            {
                await _dunnageTypeLocalRepository.SaveDunnageTypesAsync(onlineResult.Data);
                return onlineResult;
            }
        }

        return await _dunnageTypeLocalRepository.GetCachedDunnageTypesAsync();
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_DunnagePart>>> GetDunnageCatalogAsync(CancellationToken cancellationToken = default)
    {
        if (!IsOnline)
        {
            return Model_Dao_Result<List<Model_DunnagePart>>.Failure("The dunnage catalog is not available while offline.");
        }

        return await _dunnageCatalogRepository.GetDunnageCatalogAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_SetupTech_JobHistoryEntry>>> GetJobHistoryAsync(string workcenterId, CancellationToken cancellationToken = default)
    {
        if (IsOnline)
        {
            var onlineResult = await _activeJobRepository.GetJobHistoryAsync(workcenterId, cancellationToken);
            if (onlineResult.IsSuccess)
            {
                return onlineResult;
            }
        }

        return await _activeJobLocalRepository.GetJobHistoryAsync(workcenterId);
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> SetActiveJobAsync(Model_SetupTech_ActiveJob activeJob, CancellationToken cancellationToken = default)
    {
        if (!IsOnline)
        {
            return Model_Dao_Result.Failure("Active jobs cannot be changed while offline.");
        }

        var result = await _activeJobRepository.SetActiveJobAsync(activeJob, cancellationToken);
        if (result.IsSuccess)
        {
            await _activeJobLocalRepository.SetActiveJobAsync(activeJob);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> UpsertDunnageAssignmentAsync(Model_SetupTech_DunnageAssignment assignment, CancellationToken cancellationToken = default)
    {
        if (!IsOnline)
        {
            return Model_Dao_Result.Failure("Dunnage assignments cannot be changed while offline.");
        }

        var result = await _dunnageRepository.UpsertDunnageAssignmentAsync(assignment, cancellationToken);
        if (result.IsSuccess)
        {
            await _dunnageLocalRepository.UpsertDunnageAssignmentAsync(assignment);
        }

        return result;
    }
}