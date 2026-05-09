using SQLite;

namespace MTM_Waitlist_Application.Data.Local;

/// <summary>
/// Manages the SQLiteAsyncConnection for the on-device offline cache database.
/// Registered as a Singleton — the connection is created once and reused for the
/// lifetime of the application.
/// Call <see cref="InitializeAsync"/> once at startup (via DI eager resolution
/// or lazy initialisation) before any repository accesses the database.
/// </summary>
public sealed class LocalDbContext
{
    private const string DatabaseFilename = "mtm_waitlist.db3";

    // Opened on first access; SQLiteAsyncConnection is internally thread-safe.
    private SQLiteAsyncConnection? _connection;
    private Task? _initializeTask;

    /// <summary>
    /// The shared async SQLite connection.
    /// Created on first access using the platform's application data directory.
    /// </summary>
    internal SQLiteAsyncConnection Connection =>
        _connection ??= new SQLiteAsyncConnection(
            Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename),
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

    /// <summary>
    /// Creates all required tables if they do not already exist.
    /// Safe to call repeatedly; concurrent callers share the same initialization task.
    /// </summary>
    public Task InitializeAsync()
    {
        return _initializeTask ??= InitializeCoreAsync();
    }

    private async Task InitializeCoreAsync()
    {
        await Connection.CreateTableAsync<Entity_WaitlistEntry>();
        await Connection.CreateTableAsync<Entity_OfflineWriteQueue>();
    }
}
