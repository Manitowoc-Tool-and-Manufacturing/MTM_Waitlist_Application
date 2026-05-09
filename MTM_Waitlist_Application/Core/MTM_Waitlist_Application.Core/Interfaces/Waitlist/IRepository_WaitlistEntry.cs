using MTM_Waitlist_Application.Core.Models.Shared;
using MTM_Waitlist_Application.Core.Models.Waitlist;

namespace MTM_Waitlist_Application.Core.Interfaces.Waitlist;

/// <summary>
/// Contract for online waitlist data access via the backend REST API.
/// Implementations delegate exclusively to <see cref="Interfaces.Api.IApiClient"/> —
/// no connectivity checks or local-cache logic belongs here.
/// </summary>
public interface IRepository_WaitlistEntry
{
    /// <summary>Retrieves all waitlist entries from the API.</summary>
    Task<Model_Dao_Result<List<Model_WaitlistEntry>>> GetAllWaitlistEntriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves a single waitlist entry by its <paramref name="id"/> from the API.</summary>
    Task<Model_Dao_Result<Model_WaitlistEntry>> GetWaitlistEntryByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Sends a new waitlist entry to the API.</summary>
    Task<Model_Dao_Result> InsertWaitlistEntryAsync(Model_WaitlistEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Sends an updated waitlist entry to the API.</summary>
    Task<Model_Dao_Result> UpdateWaitlistEntryAsync(Model_WaitlistEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Requests deletion of the waitlist entry with the given <paramref name="id"/> from the API.</summary>
    Task<Model_Dao_Result> DeleteWaitlistEntryAsync(int id, CancellationToken cancellationToken = default);
}
