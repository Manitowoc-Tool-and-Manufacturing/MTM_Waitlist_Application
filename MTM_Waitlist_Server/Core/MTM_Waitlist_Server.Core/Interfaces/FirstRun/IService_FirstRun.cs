using MTM_Waitlist_Server.Core.Models.FirstRun;
using System.Threading;
using System.Threading.Tasks;

namespace MTM_Waitlist_Server.Core.Interfaces.FirstRun;

/// <summary>
/// Probes the environment to determine if first-run setup is required and provides
/// helpers to mark setup as complete.
/// </summary>
public interface IService_FirstRun
{
    /// <summary>
    /// Runs the three-step probe: MySQL reachable → schema exists → admin user exists.
    /// Returns a <see cref="Model_FirstRunProbeResult"/> describing what is missing, if anything.
    /// </summary>
    Task<Model_FirstRunProbeResult> ProbeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <c>true</c> when the wizard should be shown — probe did not return
    /// <see cref="FirstRunStatus.Ready"/> and <c>FirstRunComplete</c> is <c>false</c>.
    /// </summary>
    Task<bool> IsFirstRunRequiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists <c>FirstRunComplete = true</c> to <c>server-settings.json</c>.
    /// Called by the wizard after Step 3 succeeds.
    /// </summary>
    Task MarkCompleteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Step 1 of the first-run wizard.
    /// Connects using the provided privileged MySQL credentials (e.g. root), then:
    /// <list type="number">
    ///   <item>Creates the target database if it does not exist.</item>
    ///   <item>Creates the application-level MySQL user (<paramref name="appDbUsername"/>)
    ///         with full privileges on the target database.</item>
    ///   <item>Saves the connection details and app-user credentials to settings.</item>
    /// </list>
    /// </summary>
    Task<string?> SetupDatabaseAsync(
        string host,
        int port,
        string databaseName,
        string adminUsername,
        string adminPassword,
        string appDbUsername,
        string appDbPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the first admin user directly (INSERT INTO Users).
    /// Used only during first-run Step 3 — no stored procedure required for bootstrap.
    /// </summary>
    Task CreateFirstUserAsync(
        string windowsUsername,
        string appUsername,
        string displayName,
        string passwordHash,
        string role,
        CancellationToken cancellationToken = default);
}
