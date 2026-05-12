using System.Threading;
using System.Threading.Tasks;

namespace MTM_Waitlist_Server.Core.Interfaces.Api;

/// <summary>
/// Manages the lifecycle of the in-process Kestrel API host.
/// Implemented by the Admin host; consumed by ViewModels via DI.
/// </summary>
public interface IService_ApiHost
{
    /// <summary>True when the Kestrel host task is alive and has not faulted or completed.</summary>
    bool IsRunning { get; }

    /// <summary>
    /// Ensures the API host is running. If the host task has faulted, completed early,
    /// or was never started, it is rebuilt and restarted.
    /// Returns true when the host is confirmed running after the call.
    /// </summary>
    Task<bool> EnsureRunningAsync(CancellationToken ct = default);
}
