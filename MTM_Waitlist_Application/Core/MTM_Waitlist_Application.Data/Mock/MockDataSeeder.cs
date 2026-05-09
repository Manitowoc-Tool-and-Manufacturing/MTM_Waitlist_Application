#if DEBUG
using MTM_Waitlist_Application.Core.Interfaces.Waitlist;
using MTM_Waitlist_Application.Core.Models.Waitlist;

namespace MTM_Waitlist_Application.Data.Mock;

/// <summary>
/// Seeds the on-device SQLite cache with realistic-looking placeholder waitlist
/// entries so developers can work on the UI without a live API.
///
/// <para>
/// <b>When it runs:</b> only in Debug builds, and only when the fallback URL
/// (localhost) is being used AND the local database contains zero entries.
/// This prevents re-seeding once real data has been synced.
/// </para>
///
/// <para>
/// <b>How to clear seed data:</b> uninstall the app from the device/emulator
/// or delete the SQLite file at <c>FileSystem.AppDataDirectory/mtm_waitlist.db3</c>.
/// </para>
/// </summary>
public static class MockDataSeeder
{
    /// <summary>
    /// Checks whether the local SQLite store is empty and, if so, inserts a set
    /// of mock waitlist entries.  Safe to call multiple times — no-ops when data
    /// already exists.
    /// </summary>
    /// <param name="localRepository">
    ///     The local SQLite repository resolved from the DI container.
    /// </param>
    public static async Task SeedIfEmptyAsync(IRepository_WaitlistEntryLocal localRepository)
    {
        var existingResult = await localRepository.GetAllWaitlistEntriesAsync();

        // Only seed when there is truly no data; never overwrite existing entries.
        if (!existingResult.IsSuccess || existingResult.Data?.Count > 0)
        {
            return;
        }

        foreach (var entry in BuildSeedEntries())
        {
            await localRepository.InsertWaitlistEntryAsync(entry);
        }
    }

    /// <summary>
    /// Returns a representative set of mock waitlist entries.
    /// Fields will be expanded to match the real schema once the API is defined.
    /// </summary>
    private static IEnumerable<Model_WaitlistEntry> BuildSeedEntries()
    {
        // Seed IDs start at a high offset to avoid colliding with real API IDs
        // once the application is connected to the production server.
        return
        [
            new Model_WaitlistEntry { Id = 9001 },
            new Model_WaitlistEntry { Id = 9002 },
            new Model_WaitlistEntry { Id = 9003 },
            new Model_WaitlistEntry { Id = 9004 },
            new Model_WaitlistEntry { Id = 9005 },
        ];
    }
}
#endif
