using MTM_Waitlist_Server.Core.Interfaces.KillSwitch;
using MTM_Waitlist_Server.Core.Models.KillSwitch;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MTM_Waitlist_Server.Admin.Services;

/// <summary>
/// In-memory kill-switch service. All state is ephemeral — signals and heartbeats
/// are lost on restart, which is acceptable because connected clients re-register on
/// reconnect and the admin always issues a fresh signal before maintenance.
/// </summary>
internal sealed class Service_KillSwitch : IService_KillSwitch
{
    // Clients are considered connected for up to 45 seconds (15-second poll × 3 missed polls).
    private static readonly TimeSpan HeartbeatExpiry = TimeSpan.FromSeconds(45);

    // Key: "machineName|username" for ByMachine/ByUser signals, or "ALL" for global.
    private readonly ConcurrentDictionary<string, ActiveShutdownSignal> _signals = new();

    // Key: machineName (lower-invariant)
    private readonly ConcurrentDictionary<string, ClientHeartbeat> _heartbeats = new();

    /// <inheritdoc />
    public bool IsRestoreInProgress { get; set; }

    /// <inheritdoc />
    public void RecordHeartbeat(string machineName, string username, string fullName, string? workstationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(machineName);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        _heartbeats[machineName.ToLowerInvariant()] =
            new ClientHeartbeat(machineName, username, fullName, workstationName, DateTime.UtcNow);
    }

    /// <inheritdoc />
    public IReadOnlyList<ClientHeartbeat> GetConnectedClients()
    {
        var expiryCutoff = DateTime.UtcNow - HeartbeatExpiry;
        return _heartbeats.Values
            .Where(h => h.LastSeenUtc >= expiryCutoff)
            .OrderBy(h => h.MachineName)
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<ActiveShutdownSignal> GetActiveSignals() =>
        _signals.Values.ToList();

    /// <inheritdoc />
    public void SetShutdownSignal(
        ShutdownTarget target,
        int warningSeconds,
        string message,
        string? machineName = null,
        string? username = null)
    {
        var key = BuildKey(target, machineName, username);

        // Debounce — do not overwrite an existing signal for the same key.
        if (_signals.ContainsKey(key))
        {
            return;
        }

        var signal = new ActiveShutdownSignal(
            target, machineName, username, warningSeconds, message, DateTime.UtcNow);

        _signals.TryAdd(key, signal);
    }

    /// <inheritdoc />
    public void CancelAllSignals() => _signals.Clear();

    /// <inheritdoc />
    public ActiveShutdownSignal? GetSignalForClient(string machineName, string username)
    {
        // Global signal applies to everyone.
        if (_signals.TryGetValue("ALL", out var global))
        {
            return global;
        }

        // Machine-specific signal.
        var machineKey = BuildKey(ShutdownTarget.ByMachine, machineName);
        if (_signals.TryGetValue(machineKey, out var machineSignal))
        {
            return machineSignal;
        }

        // User-specific signal.
        var userKey = BuildKey(ShutdownTarget.ByUser, username: username);
        if (_signals.TryGetValue(userKey, out var userSignal))
        {
            return userSignal;
        }

        return null;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static string BuildKey(ShutdownTarget target, string? machineName = null, string? username = null) =>
        target switch
        {
            ShutdownTarget.All => "ALL",
            ShutdownTarget.ByMachine => $"MACHINE|{machineName?.ToLowerInvariant()}",
            ShutdownTarget.ByUser => $"USER|{username?.ToLowerInvariant()}",
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
        };
}
