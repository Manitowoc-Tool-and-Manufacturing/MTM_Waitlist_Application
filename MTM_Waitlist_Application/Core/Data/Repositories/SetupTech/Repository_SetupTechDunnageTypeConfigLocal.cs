using Core.Interfaces.SetupTech;
using Core.Models.SetupTech;
using Core.Models.Shared;
using Data.Local;

namespace Data.Repositories.SetupTech;

/// <summary>
/// Offline Setup Tech repository for caching enabled dunnage type configuration locally.
/// </summary>
public sealed class Repository_SetupTechDunnageTypeConfigLocal : IRepository_SetupTechDunnageTypeConfigLocal
{
    private readonly LocalDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository_SetupTechDunnageTypeConfigLocal"/> class.
    /// </summary>
    public Repository_SetupTechDunnageTypeConfigLocal(LocalDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_SetupTech_DunnageTypeConfig>>> GetCachedDunnageTypesAsync()
    {
        try
        {
            await _context.InitializeAsync();
            var entities = await _context.Connection.Table<Entity_SetupTech_DunnageTypeConfig>()
                .OrderBy(type => type.DisplayOrder)
                .ThenBy(type => type.DunnageTypeName)
                .ToListAsync();

            var dunnageTypes = entities.Select(MapToModel).ToList();
            return Model_Dao_Result<List<Model_SetupTech_DunnageTypeConfig>>.Success(dunnageTypes);
        }
        catch (Exception ex)
        {
            return Model_Dao_Result<List<Model_SetupTech_DunnageTypeConfig>>.Failure($"Failed to retrieve cached dunnage types: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> SaveDunnageTypesAsync(List<Model_SetupTech_DunnageTypeConfig> dunnageTypes)
    {
        try
        {
            await _context.InitializeAsync();
            await _context.Connection.DeleteAllAsync<Entity_SetupTech_DunnageTypeConfig>();

            var entities = dunnageTypes.Select(MapToEntity).ToList();
            if (entities.Count > 0)
            {
                await _context.Connection.InsertAllAsync(entities);
            }

            return Model_Dao_Result.Success();
        }
        catch (Exception ex)
        {
            return Model_Dao_Result.Failure($"Failed to cache dunnage types: {ex.Message}");
        }
    }

    private static Model_SetupTech_DunnageTypeConfig MapToModel(Entity_SetupTech_DunnageTypeConfig entity)
        => new()
        {
            DunnageTypeId = entity.DunnageTypeId,
            DunnageTypeName = entity.DunnageTypeName,
            IsEnabled = entity.IsEnabled,
            DisplayOrder = entity.DisplayOrder,
        };

    private static Entity_SetupTech_DunnageTypeConfig MapToEntity(Model_SetupTech_DunnageTypeConfig model)
        => new()
        {
            DunnageTypeId = model.DunnageTypeId,
            DunnageTypeName = model.DunnageTypeName,
            IsEnabled = model.IsEnabled,
            DisplayOrder = model.DisplayOrder,
        };
}