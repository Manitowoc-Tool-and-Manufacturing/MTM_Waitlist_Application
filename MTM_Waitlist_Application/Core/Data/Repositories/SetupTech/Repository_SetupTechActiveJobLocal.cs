using System.Text.Json;
using Core.Interfaces.SetupTech;
using Core.Models.SetupTech;
using Core.Models.Shared;
using Data.Local;

namespace Data.Repositories.SetupTech;

/// <summary>
/// Offline Setup Tech repository for caching active jobs and workstation job history locally.
/// </summary>
public sealed class Repository_SetupTechActiveJobLocal : IRepository_SetupTechActiveJobLocal
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly LocalDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository_SetupTechActiveJobLocal"/> class.
    /// </summary>
    public Repository_SetupTechActiveJobLocal(LocalDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<Model_SetupTech_ActiveJob>> GetActiveJobAsync(string workcenterId)
    {
        try
        {
            await _context.InitializeAsync();
            var entity = await _context.Connection.Table<Entity_SetupTech_ActiveJob>()
                .Where(job => job.WorkcenterId == workcenterId)
                .FirstOrDefaultAsync();

            if (entity is null)
            {
                return Model_Dao_Result<Model_SetupTech_ActiveJob>.Failure($"No cached active job was found for workcenter '{workcenterId}'.");
            }

            return Model_Dao_Result<Model_SetupTech_ActiveJob>.Success(MapToModel(entity));
        }
        catch (Exception ex)
        {
            return Model_Dao_Result<Model_SetupTech_ActiveJob>.Failure($"Failed to retrieve cached active job: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_SetupTech_JobHistoryEntry>>> GetJobHistoryAsync(string workcenterId)
    {
        try
        {
            await _context.InitializeAsync();
            var entities = await _context.Connection.Table<Entity_SetupTech_JobHistoryEntry>()
                .Where(entry => entry.WorkcenterId == workcenterId)
                .OrderByDescending(entry => entry.ActiveUntil)
                .ToListAsync();

            var history = entities.Select(MapToModel).ToList();
            return Model_Dao_Result<List<Model_SetupTech_JobHistoryEntry>>.Success(history);
        }
        catch (Exception ex)
        {
            return Model_Dao_Result<List<Model_SetupTech_JobHistoryEntry>>.Failure($"Failed to retrieve cached job history: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> SetActiveJobAsync(Model_SetupTech_ActiveJob activeJob)
    {
        try
        {
            await _context.InitializeAsync();

            var existing = await _context.Connection.Table<Entity_SetupTech_ActiveJob>()
                .Where(job => job.WorkcenterId == activeJob.WorkcenterId)
                .FirstOrDefaultAsync();

            if (existing is not null)
            {
                var historyEntity = new Entity_SetupTech_JobHistoryEntry
                {
                    Id = existing.Id,
                    WorkcenterId = existing.WorkcenterId,
                    WorkOrderId = existing.WorkOrderId,
                    SequenceNo = existing.SequenceNo,
                    PartId = existing.PartId,
                    PartType = existing.PartType,
                    SubordinatePartsJson = existing.SubordinatePartsJson,
                    DunnageAssignmentsJson = existing.DunnageAssignmentsJson,
                    SetupTechUserId = existing.SetupTechUserId,
                    ActiveFrom = existing.ActiveSince,
                    ActiveUntil = DateTime.UtcNow,
                };

                await _context.Connection.InsertAsync(historyEntity);
            }

            await _context.Connection.InsertOrReplaceAsync(MapToEntity(activeJob));
            return Model_Dao_Result.Success();
        }
        catch (Exception ex)
        {
            return Model_Dao_Result.Failure($"Failed to cache active job: {ex.Message}");
        }
    }

    private static Model_SetupTech_ActiveJob MapToModel(Entity_SetupTech_ActiveJob entity)
        => new()
        {
            Id = entity.Id,
            WorkcenterId = entity.WorkcenterId,
            WorkOrderId = entity.WorkOrderId,
            SequenceNo = entity.SequenceNo,
            PartId = entity.PartId,
            PartType = entity.PartType,
            SubordinateParts = DeserializeList<Model_SetupTech_SubordinatePart>(entity.SubordinatePartsJson),
            DunnageAssignments = DeserializeList<Model_SetupTech_DunnageAssignment>(entity.DunnageAssignmentsJson),
            SetupTechUserId = entity.SetupTechUserId,
            ActiveSince = entity.ActiveSince,
        };

    private static Model_SetupTech_JobHistoryEntry MapToModel(Entity_SetupTech_JobHistoryEntry entity)
        => new()
        {
            Id = entity.Id,
            WorkcenterId = entity.WorkcenterId,
            WorkOrderId = entity.WorkOrderId,
            SequenceNo = entity.SequenceNo,
            PartId = entity.PartId,
            PartType = entity.PartType,
            SubordinateParts = DeserializeList<Model_SetupTech_SubordinatePart>(entity.SubordinatePartsJson),
            DunnageAssignments = DeserializeList<Model_SetupTech_DunnageAssignment>(entity.DunnageAssignmentsJson),
            SetupTechUserId = entity.SetupTechUserId,
            ActiveFrom = entity.ActiveFrom,
            ActiveUntil = entity.ActiveUntil,
        };

    private static Entity_SetupTech_ActiveJob MapToEntity(Model_SetupTech_ActiveJob model)
        => new()
        {
            Id = model.Id,
            WorkcenterId = model.WorkcenterId,
            WorkOrderId = model.WorkOrderId,
            SequenceNo = model.SequenceNo,
            PartId = model.PartId,
            PartType = model.PartType,
            SubordinatePartsJson = JsonSerializer.Serialize(model.SubordinateParts, SerializerOptions),
            DunnageAssignmentsJson = JsonSerializer.Serialize(model.DunnageAssignments, SerializerOptions),
            SetupTechUserId = model.SetupTechUserId,
            ActiveSince = model.ActiveSince,
        };

    private static List<TItem> DeserializeList<TItem>(string json)
        => JsonSerializer.Deserialize<List<TItem>>(json, SerializerOptions) ?? [];
}