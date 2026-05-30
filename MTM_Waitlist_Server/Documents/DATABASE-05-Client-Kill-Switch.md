# DATABASE-05: Client Kill Switch (Remote Application Shutdown)

---

## Confirmed Decisions

| # | Question | Decision |
|---|---|---|
| 1 | What does "immediately" mean? | **15-second grace period** with a non-dismissable countdown overlay on the client. True zero-warning shutdown is never used |
| 2 | In-flight wizard state on kill? | **Lost.** The operator restarts the wizard after reconnect. Acceptable for maintenance windows |
| 3 | Individual targets or all-at-once only? | **Both.** Target by (a) specific machine name (from JWT), (b) specific user, (c) all clients |
| 4 | Signal delivery technology? | **Polling** — clients poll `GET /api/admin/shutdown-signal` every 15 seconds. No SignalR required |
| 5 | Kill signal storage during restore? | **In-memory only** (not MySQL). Kill buttons in the admin UI are **disabled** while a restore is in progress so they cannot be accidentally re-triggered. Buttons are also **debounced** — after issuing a signal, the same button cannot be pressed again until the current signal is resolved or cancelled |

---

## Overview

The Kill Switch module allows an IT admin to remotely shut down the MAUI Waitlist Application on any or all connected client devices. Primary use cases:

- **Database maintenance** — shut down all clients before a restore, migration, or server restart.
- **Misbehaving device** — disconnect a specific workstation that is stuck or causing errors.
- **End of shift** — optional mass shutdown at shift end (not likely needed but requested).

The mechanism is simple: the API exposes an in-memory “shutdown signal” endpoint. Client apps (WinUI 3 on Windows, MAUI on Android) poll this endpoint every 15 seconds as part of their normal session keepalive. When a shutdown signal is present for their device or is global, the client displays a countdown overlay and then closes.

---

## Kill Switch Architecture

### Server Side (Admin App + API)

The kill signal is stored in a thread-safe in-memory structure on the API server:

```csharp
public interface IService_KillSwitch
{
    // Set a shutdown signal (global or targeted). Returns false if a signal is already active for this target.
    bool SetShutdownSignal(ShutdownTarget target, int warningSeconds, string message = "");

    // Check if a shutdown signal applies to a given client
    ShutdownSignal? GetSignalForClient(string machineName, string username);

    // Cancel a pending shutdown
    void CancelShutdown(ShutdownTarget target);

    // Get all active signals for the admin UI
    IReadOnlyList<ActiveShutdownSignal> GetActiveSignals();

    // True when a restore is in progress (externally set by Service_Restore)
    bool IsRestoreInProgress { get; set; }
}

public enum ShutdownTargetType { All, ByMachine, ByUser }

public record ShutdownTarget(ShutdownTargetType Type, string? MachineNameOrUsername = null);

public record ShutdownSignal(int WarningSeconds, DateTime IssuedAt);

public record ActiveShutdownSignal(ShutdownTarget Target, ShutdownSignal Signal, int ClientsAffected);
```

The in-memory store uses `ConcurrentDictionary` — no database persistence. The kill signal must be sent before the API is stopped for a restore (automated by the restore workflow in DATABASE-04).

**Debounce rule:** Once a shutdown signal is set for a target, the same target cannot receive another signal until the current one is resolved (all clients disconnected) or explicitly cancelled. The `IService_KillSwitch` implementation enforces this by checking for an existing signal before accepting a new `SetShutdownSignal` call for the same target.

### API Endpoint

```
GET /api/admin/shutdown-signal
```

**Authentication:** Same JWT as all other endpoints. The client app calls this endpoint as part of its normal 15-second session poll.

**Response when no signal applies:**
```json
{ "shutdown": false }
```

**Response when a signal applies:**
```json
{
  "shutdown": true,
  "warningSeconds": 300,
  "issuedAt": "2026-05-10T14:30:00Z",
  "message": "Server maintenance at 3:30 PM. Please save your work."
}
```

The endpoint reads the JWT `MachineName` claim and `Username` claim to determine which signals apply.

### Client Side (MAUI App — existing `SyncService` extension)

The MAUI app's `SyncService` already polls the API on a timer. Extend it to also check `GET /api/admin/shutdown-signal` on each poll.

When `shutdown: true` is received:

```csharp
// SyncService.cs — existing polling loop, new check added
var signal = await _apiClient.GetAsync<ShutdownSignalResponse>("/api/admin/shutdown-signal");
if (signal.IsSuccess && signal.Data.Shutdown)
{
    await _shutdownService.BeginCountdownAsync(
        signal.Data.WarningSeconds,
        signal.Data.Message);
}
```

`IService_ClientShutdown.BeginCountdownAsync()` shows a non-dismissable overlay on the current page:

```
┌─────────────────────────────────────────────────────┐
│  ⚠ SERVER MAINTENANCE                               │
│                                                     │
│  Server maintenance at 3:30 PM. Please save work.  │
│                                                     │
│  This application will close in:                    │
│                                                     │
│              4:47                                   │
│           (minutes:seconds)                         │
│                                                     │
│  Your current session will be restored when the    │
│  server comes back online.                         │
└─────────────────────────────────────────────────────┘
```

When the countdown reaches zero, `Application.Current.Quit()` is called.

---

## Admin UI — Kill Switch Module

```
┌─────────────────────────────────────────────────────────────────┐
│  KILL SWITCH — CLIENT SHUTDOWN MANAGEMENT                       │
├─────────────────────────────────────────────────────────────────┤
│  CONNECTED CLIENTS (3)                                          │
│  ┌────────────────┬───────────────┬──────────────┬───────────┐  │
│  │ Machine        │ User          │ Last Seen    │ Actions   │  │
│  ├────────────────┼───────────────┼──────────────┼───────────┤  │
│  │ KIOSK-PRESS14  │ operator_tom  │ 12 sec ago   │ [Kill →]  │  │
│  │ KIOSK-PRESS07  │ operator_ann  │ 4 sec ago    │ [Kill →]  │  │
│  │ TABLET-FLOOR01 │ handler_mike  │ 8 sec ago    │ [Kill →]  │  │
│  └────────────────┴───────────────┴──────────────┴───────────┘  │
├─────────────────────────────────────────────────────────────────┤
│  GLOBAL SHUTDOWN                                                │
│                                                                 │
│  Warning time:  [Immediately (15 sec)] [▼]                      │
│  Message:  [Server maintenance. Application will restart.]      │
│                                                                 │
│  [Shut Down All Clients]          [Cancel Active Shutdown]      │
├─────────────────────────────────────────────────────────────────┤
│  ACTIVE SHUTDOWN SIGNALS (0)                                    │
│  No active shutdown signals.                                    │
└─────────────────────────────────────────────────────────────────┘
```

**Button states:**
- **`[Kill →]` (individual)** and **`[Shut Down All Clients]`** are disabled when `IsRestoreInProgress = true` or when an active signal already exists for that target (debounce).
- Once a signal is sent, the relevant button becomes disabled until the signal is resolved (clients disconnect) or `[Cancel Active Shutdown]` is pressed.
- A tooltip on a disabled button explains why: `"Restore in progress — kill switch locked"` or `"Shutdown signal already active for this client."`

### Warning Time Dropdown Options

| Option | Warning seconds |
|---|---|
| Immediately (15 sec) | 15 |
| 1 minute | 60 |
| 5 minutes | 300 |
| 10 minutes | 600 |
| 30 minutes | 1800 |
| Custom... | (text field, seconds) |

### Individual Kill

Row-level "Kill →" button on each client row opens a smaller confirmation:

```
Shut down KIOSK-PRESS14 (operator_tom)?
Warning time: [5 minutes ▼]
[Cancel]  [Shut Down This Client]
```

---

## How "Connected Clients" Are Tracked

The API uses the in-memory `IService_KillSwitch` service and the same `GET /api/admin/shutdown-signal` endpoint to infer connected clients. Every client app that successfully calls this endpoint within the last 30 seconds is considered “connected.”

The API logs each call's `MachineName` + `Username` (from JWT) + `DateTime.UtcNow` to a `ConcurrentDictionary<string, ClientHeartbeat>`. The admin Kill Switch module reads this dictionary:

```csharp
public record ClientHeartbeat(string MachineName, string Username, DateTime LastSeenUtc);
```

A client is shown as "connected" if `DateTime.UtcNow - LastSeenUtc < 30 seconds`.

`MachineName` comes from the JWT claim (added by the API during login — the MAUI app sends `Environment.MachineName` as part of the login request body).

---

## Changes to Client App Required

1. **Login request:** Add `MachineName = Environment.MachineName` to the login request payload. The API stores this in the JWT as a claim.
2. **SyncService:** Add `GET /api/admin/shutdown-signal` to the 15-second polling loop.
3. **New service:** `IService_ClientShutdown` with `BeginCountdownAsync(int seconds, string message)`. Registered in `AddSharedServices()`.
4. **Shutdown overlay:**
   - **Windows (WinUI 3):** A non-dismissable `ContentDialog` (or a full-cover overlay `Grid` in `MainWindow`) displayed when countdown begins. No escape route.
   - **Android (MAUI):** A non-dismissable `ContentPage` overlay pushed onto the MAUI navigation stack when countdown begins. No back-button escape.

---

## New API Endpoint: `AdminController`

```csharp
[ApiController]
[Route("api/admin")]
[Authorize]  // requires any valid JWT
public class AdminController : ControllerBase
{
    // GET api/admin/shutdown-signal — called by client apps every 15 sec
    [HttpGet("shutdown-signal")]
    public IActionResult GetShutdownSignal()

    // POST api/admin/shutdown — called by admin UI to set a signal
    [HttpPost("shutdown")]
    [Authorize(Roles = "Admin,Developer")]  // only admin/dev JWT can set signals
    public IActionResult SetShutdown([FromBody] SetShutdownRequest request)

    // DELETE api/admin/shutdown — cancel all or targeted signals
    [HttpDelete("shutdown")]
    [Authorize(Roles = "Admin,Developer")]
    public IActionResult CancelShutdown()

    // GET api/admin/clients — returns known connected clients for the admin UI
    [HttpGet("clients")]
    [Authorize(Roles = "Admin,Developer")]
    public IActionResult GetConnectedClients()
}
```

Note: The admin UI calls these same API endpoints through `localhost:5000` rather than querying the in-memory service directly. This keeps the kill switch logic in one place (the API service layer) and makes the kill switch testable independently of the WinUI layer.

---

## ViewModel

```
Module.KillSwitch/
  ViewModels/
    ViewModel_KillSwitch.cs
  Views/
    View_KillSwitch.xaml
    View_KillSwitch.xaml.cs
```

Key properties:
```csharp
[ObservableProperty] ObservableCollection<ClientHeartbeat> _connectedClients
[ObservableProperty] ObservableCollection<ActiveShutdownSignal> _activeSignals
[ObservableProperty] int _selectedWarningSeconds
[ObservableProperty] string _shutdownMessage
[ObservableProperty] bool _isShutdownActive       // true when any signal is pending
[ObservableProperty] bool _isRestoreInProgress    // true while restore is running — locks all kill buttons
```

Key commands:
```csharp
[RelayCommand(CanExecute = nameof(CanIssueShutdown))] ShutDownAllAsync()
[RelayCommand(CanExecute = nameof(CanIssueShutdown))] ShutDownClientAsync(ClientHeartbeat client)
[RelayCommand] CancelShutdownAsync()
[RelayCommand] RefreshClientsAsync()
```

`CanIssueShutdown` returns `false` when `IsRestoreInProgress` is `true` or when an active signal already exists for the target. `NotifyCanExecuteChanged()` is called whenever `_isRestoreInProgress` or `_activeSignals` changes.

Auto-refresh: the connected clients list refreshes every 5 seconds (shorter than the 30-second expiry to ensure the list is visually current).

---

## Integration with Restore Workflow (DATABASE-04)

When the restore wizard runs, `Service_Restore` sets `IService_KillSwitch.IsRestoreInProgress = true` before broadcasting the shutdown signal. This immediately disables all kill switch buttons in the admin UI (via `_isRestoreInProgress` observable property). The flag is cleared after the API restarts successfully.

The restore proceeds after either:
- All clients disconnect (all heartbeats gone from the dictionary), **or**
- The countdown expires (300 seconds)

This is handled in `Service_Restore.RunRestoreAsync()`:
```csharp
_killSwitch.SetShutdownSignal(ShutdownTarget.All, 300);
await WaitForClientsToDisconnectAsync(maxWaitSeconds: 310, ct);
await StopApiAsync();
// ... run mysqldump ...
await StartApiAsync();
```

---

## Open Decisions

- **JWT `MachineName` claim security:** For a closed internal network this is acceptable. If strict targeting is needed, validate machine name against the `SharedWorkstations` table.
- **Kill switch when API is unreachable:** Clients see a connection warning in the MAUI app's status bar but will not crash. Intended behavior — Kestrel restarts after maintenance.
- **Auditing kill switch use:** For v1, log to the in-process activity log (ephemeral). A `ServerEvents` table audit trail is a v2 enhancement.
