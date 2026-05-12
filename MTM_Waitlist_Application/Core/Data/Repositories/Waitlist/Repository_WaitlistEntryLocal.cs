using System.Text.Json;
using Core.Interfaces.Waitlist;
using Core.Models.Shared;
using Core.Models.Waitlist;
using Data.Local;

namespace Data.Repositories.Waitlist;

/// <summary>
/// Offline implementation of <see cref="IRepository_WaitlistEntryLocal"/> that
/// stores waitlist records in the on-device SQLite database via <see cref="LocalDbContext"/>.
/// No connectivity checks are performed here — that responsibility belongs
/// to the Service layer.  All SQLite exceptions are caught internally and
/// returned as <see cref="Model_Dao_Result"/> failures.
/// Also manages the offline write queue used by the sync service.
/// </summary>
public sealed class Repository_WaitlistEntryLocal : IRepository_WaitlistEntryLocal
{
    private readonly LocalDbContext _context;

    /// <summary>
    /// Initialises a new instance with the supplied local database context.
    /// </summary>
    public Repository_WaitlistEntryLocal(LocalDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<Model_WaitlistEntry>>> GetAllWaitlistEntriesAsync()
    {
        try
        {
            await _context.InitializeAsync();
            var entities = await _context.Connection.Table<Entity_WaitlistEntry>().ToListAsync();
            var models = entities.Select(MapToModel).ToList();
            return Model_Dao_Result<List<Model_WaitlistEntry>>.Success(models);
        }
        catch (Exception ex)
        {
            return Model_Dao_Result<List<Model_WaitlistEntry>>.Failure(
                $"Failed to retrieve local waitlist entries: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<Model_WaitlistEntry>> GetWaitlistEntryByIdAsync(int id)
    {
        try
        {
            await _context.InitializeAsync();
            var entity = await _context.Connection.Table<Entity_WaitlistEntry>()
                .Where(e => e.Id == id)
                .FirstOrDefaultAsync();

            if (entity is null)
            {
                return Model_Dao_Result<Model_WaitlistEntry>.Failure($"Local entry with id {id} not found.");
            }

            return Model_Dao_Result<Model_WaitlistEntry>.Success(MapToModel(entity));
        }
        catch (Exception ex)
        {
            return Model_Dao_Result<Model_WaitlistEntry>.Failure(
                $"Failed to retrieve local entry {id}: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> InsertWaitlistEntryAsync(Model_WaitlistEntry entry)
    {
        try
        {
            await _context.InitializeAsync();
            await _context.Connection.InsertAsync(MapToEntity(entry));
            return Model_Dao_Result.Success();
        }
        catch (Exception ex)
        {
            return Model_Dao_Result.Failure($"Failed to insert local entry: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> UpdateWaitlistEntryAsync(Model_WaitlistEntry entry)
    {
        try
        {
            await _context.InitializeAsync();
            await _context.Connection.UpdateAsync(MapToEntity(entry));
            return Model_Dao_Result.Success();
        }
        catch (Exception ex)
        {
            return Model_Dao_Result.Failure($"Failed to update local entry: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> DeleteWaitlistEntryAsync(int id)
    {
        try
        {
            await _context.InitializeAsync();
            await _context.Connection.DeleteAsync<Entity_WaitlistEntry>(id);
            return Model_Dao_Result.Success();
        }
        catch (Exception ex)
        {
            return Model_Dao_Result.Failure($"Failed to delete local entry {id}: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> EnqueuePendingWriteAsync(Model_WaitlistEntry entry, string operation)
    {
        try
        {
            await _context.InitializeAsync();
            var queueItem = new Entity_OfflineWriteQueue
            {
                EntryId = entry.Id,
                EntryJson = JsonSerializer.Serialize(entry),
                Operation = operation,
                EnqueuedAt = DateTimeOffset.UtcNow
            };

            await _context.Connection.InsertAsync(queueItem);
            return Model_Dao_Result.Success();
        }
        catch (Exception ex)
        {
            return Model_Dao_Result.Failure($"Failed to enqueue pending write: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<List<(int QueueId, Model_WaitlistEntry Entry, string Operation)>>> GetPendingWritesAsync()
    {
        try
        {
            await _context.InitializeAsync();
            var queueItems = await _context.Connection.Table<Entity_OfflineWriteQueue>()
                .OrderBy(q => q.EnqueuedAt)
                .ToListAsync();

            var results = queueItems
                .Select(q =>
                {
                    var entry = JsonSerializer.Deserialize<Model_WaitlistEntry>(q.EntryJson)
                        ?? new Model_WaitlistEntry { Id = q.EntryId };
                    return (q.QueueId, entry, q.Operation);
                })
                .ToList();

            return Model_Dao_Result<List<(int, Model_WaitlistEntry, string)>>.Success(results);
        }
        catch (Exception ex)
        {
            return Model_Dao_Result<List<(int, Model_WaitlistEntry, string)>>.Failure(
                $"Failed to retrieve pending writes: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> ClearPendingWriteAsync(int queueId)
    {
        try
        {
            await _context.InitializeAsync();
            await _context.Connection.DeleteAsync<Entity_OfflineWriteQueue>(queueId);
            return Model_Dao_Result.Success();
        }
        catch (Exception ex)
        {
            return Model_Dao_Result.Failure($"Failed to clear pending write {queueId}: {ex.Message}");
        }
    }

    // ── Mapping helpers ────────────────────────────────────────────────────────

    private static Model_WaitlistEntry MapToModel(Entity_WaitlistEntry entity) =>
        new() { Id = entity.Id };

    private static Entity_WaitlistEntry MapToEntity(Model_WaitlistEntry model) =>
        new() { Id = model.Id };
}
