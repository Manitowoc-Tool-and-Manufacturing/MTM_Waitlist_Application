using Core.Interfaces.SetupTech;
using Core.Models.SetupTech;
using Core.Models.Shared;
using Data.Local;

namespace Data.Repositories.SetupTech;

/// <summary>
/// Offline Setup Tech repository for cached work-order dunnage assignments.
/// </summary>
public sealed class Repository_WorkOrderDunnageLocal : IRepository_WorkOrderDunnageLocal
{
    private readonly LocalDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository_WorkOrderDunnageLocal"/> class.
    /// </summary>
    public Repository_WorkOrderDunnageLocal(LocalDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> DeleteDunnageAssignmentAsync(int assignmentId)
    {
        try
        {
            await _context.InitializeAsync();
            var entity = await _context.Connection.Table<Entity_SetupTech_DunnageAssignment>()
                .Where(assignment => assignment.AssignmentId == assignmentId)
                .FirstOrDefaultAsync();

            if (entity is null)
            {
                return Model_Dao_Result.Failure($"No cached dunnage assignment was found for id {assignmentId}.");
            }

            await _context.Connection.DeleteAsync(entity);
            return Model_Dao_Result.Success();
        }
        catch (Exception ex)
        {
            return Model_Dao_Result.Failure($"Failed to remove cached dunnage assignment: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_SetupTech_DunnageAssignment>>> GetDunnageAssignmentAsync(string workOrderId, int sequenceNo)
    {
        try
        {
            await _context.InitializeAsync();
            var entities = await _context.Connection.Table<Entity_SetupTech_DunnageAssignment>()
                .Where(assignment => assignment.WorkOrderId == workOrderId && assignment.SequenceNo == sequenceNo)
                .OrderBy(assignment => assignment.DunnageTypeName)
                .ThenBy(assignment => assignment.DunnagePartName)
                .ToListAsync();

            var assignments = entities.Select(MapToModel).ToList();
            return Model_Dao_Result<List<Model_SetupTech_DunnageAssignment>>.Success(assignments);
        }
        catch (Exception ex)
        {
            return Model_Dao_Result<List<Model_SetupTech_DunnageAssignment>>.Failure($"Failed to retrieve cached dunnage assignments: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> UpsertDunnageAssignmentAsync(Model_SetupTech_DunnageAssignment assignment)
    {
        try
        {
            await _context.InitializeAsync();
            await _context.Connection.InsertOrReplaceAsync(MapToEntity(assignment));
            return Model_Dao_Result.Success();
        }
        catch (Exception ex)
        {
            return Model_Dao_Result.Failure($"Failed to cache dunnage assignment: {ex.Message}");
        }
    }

    private static Model_SetupTech_DunnageAssignment MapToModel(Entity_SetupTech_DunnageAssignment entity)
        => new()
        {
            AssignmentId = entity.AssignmentId,
            WorkOrderId = entity.WorkOrderId,
            SequenceNo = entity.SequenceNo,
            DunnagePartId = entity.DunnagePartId,
            DunnagePartName = entity.DunnagePartName,
            DunnageTypeId = entity.DunnageTypeId,
            DunnageTypeName = entity.DunnageTypeName,
            LastModifiedByUserId = entity.LastModifiedByUserId,
        };

    private static Entity_SetupTech_DunnageAssignment MapToEntity(Model_SetupTech_DunnageAssignment model)
        => new()
        {
            CacheKey = CreateCacheKey(model.WorkOrderId, model.SequenceNo, model.DunnagePartId),
            AssignmentId = model.AssignmentId,
            WorkOrderId = model.WorkOrderId,
            SequenceNo = model.SequenceNo,
            DunnagePartId = model.DunnagePartId,
            DunnagePartName = model.DunnagePartName,
            DunnageTypeId = model.DunnageTypeId,
            DunnageTypeName = model.DunnageTypeName,
            LastModifiedByUserId = model.LastModifiedByUserId,
        };

    private static string CreateCacheKey(string workOrderId, int sequenceNo, int dunnagePartId)
        => $"{workOrderId}:{sequenceNo}:{dunnagePartId}";
}