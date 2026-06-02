using Core.Interfaces.InforVisual;
using Core.Models.InforVisual;
using Core.Models.Shared;
using Data.Local;

namespace Data.Repositories.InforVisual;

/// <summary>
/// Offline Infor Visual repository that caches workcenters in local SQLite storage.
/// </summary>
public sealed class Repository_InforVisualLocal : IRepository_InforVisualLocal
{
    private readonly LocalDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository_InforVisualLocal"/> class.
    /// </summary>
    public Repository_InforVisualLocal(LocalDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_VisualWorkcenter>>> GetCachedWorkcentersAsync()
    {
        try
        {
            await _context.InitializeAsync();
            var entities = await _context.Connection.Table<Entity_VisualWorkcenter>()
                .OrderBy(workcenter => workcenter.WorkcenterId)
                .ToListAsync();

            var workcenters = entities.Select(MapToModel).ToList();
            return Model_Dao_Result<List<Model_VisualWorkcenter>>.Success(workcenters);
        }
        catch (Exception ex)
        {
            return Model_Dao_Result<List<Model_VisualWorkcenter>>.Failure($"Failed to retrieve cached workcenters: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> SaveWorkcentersAsync(List<Model_VisualWorkcenter> workcenters)
    {
        try
        {
            await _context.InitializeAsync();
            await _context.Connection.DeleteAllAsync<Entity_VisualWorkcenter>();

            var entities = workcenters.Select(MapToEntity).ToList();
            if (entities.Count > 0)
            {
                await _context.Connection.InsertAllAsync(entities);
            }

            return Model_Dao_Result.Success();
        }
        catch (Exception ex)
        {
            return Model_Dao_Result.Failure($"Failed to cache workcenters: {ex.Message}");
        }
    }

    private static Model_VisualWorkcenter MapToModel(Entity_VisualWorkcenter entity)
        => new()
        {
            WorkcenterId = entity.WorkcenterId,
            Description = entity.Description,
            ResourceType = entity.ResourceType,
            DepartmentId = entity.DepartmentId,
            ScheduleGroup = entity.ScheduleGroup,
        };

    private static Entity_VisualWorkcenter MapToEntity(Model_VisualWorkcenter model)
        => new()
        {
            WorkcenterId = model.WorkcenterId,
            Description = model.Description,
            ResourceType = model.ResourceType,
            DepartmentId = model.DepartmentId,
            ScheduleGroup = model.ScheduleGroup,
        };
}