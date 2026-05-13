using MTM_Waitlist_Server.Core.Models.KillSwitch;

namespace MTM_Waitlist_Server.Core.Interfaces.KillSwitch;

/// <summary>Manages graceful shutdown signals sent to connected MAUI clients.</summary>
public interface IService_KillSwitch
{
    /// <summary>True while a restore operation is in progress — disables issuing new signals.</summary>
    bool IsRestoreInProgress { get; set; }

    /// <summary>Registers or refreshes a client heartbeat.</summary>
    void RecordHeartbeat(string machineName, string username, string fullName, string? workstationName);

    /// <summary>Removes a client heartbeat when the client disconnects normally.</summary>
    void RemoveHeartbeat(string machineName, string username);

    /// <summary>Returns all clients whose last heartbeat is within the expiry window.</summary>
    IReadOnlyList<ClientHeartbeat> GetConnectedClients();

    /// <summary>Returns all currently active shutdown signals.</summary>
    IReadOnlyList<ActiveShutdownSignal> GetActiveSignals();

    /// <summary>Issues a shutdown signal targeting all, a machine, or a user.</summary>
    void SetShutdownSignal(ShutdownTarget target, int warningSeconds, string message,
        string? machineName = null, string? username = null);

    /// <summary>Cancels all active shutdown signals.</summary>
    void CancelAllSignals();

    /// <summary>Returns the shutdown signal applicable to the caller, or null if none.</summary>
    ActiveShutdownSignal? GetSignalForClient(string machineName, string username);

    /// <summary>Clears the shutdown signal that applies to a client after the client receives it.</summary>
    void AcknowledgeSignalForClient(string machineName, string username);
}
