using MTM_Waitlist_Server.Core.Models.Splash;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MTM_Waitlist_Server.Core.Interfaces.Splash;

/// <summary>
/// Orchestrates all startup steps for the server admin application, reporting progress via
/// <see cref="IProgress{T}"/> and returning a <see cref="StartupOutcome"/> that determines
/// which <c>MainWindow</c> launch mode to activate.
/// </summary>
public interface IService_StartupCoordinator
{
    /// <summary>
    /// Runs all startup steps asynchronously.  Never throws — all exceptions are caught
    /// internally and converted to an appropriate <see cref="StartupOutcome"/>.
    /// </summary>
    /// <param name="progress">Receives a <see cref="StartupStep"/> after each step transition.</param>
    /// <param name="ct">Cancellation token; cancelling returns <see cref="StartupOutcome.Cancelled"/>.</param>
    Task<StartupOutcome> RunAsync(IProgress<StartupStep> progress, CancellationToken ct);
}
