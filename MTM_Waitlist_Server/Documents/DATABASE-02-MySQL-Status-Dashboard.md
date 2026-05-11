# DATABASE-02: MySQL Status Dashboard

---

## Confirmed Decisions

| # | Question | Decision |
|---|---|---|
| 1 | Which MySQL user does the admin app connect as? | **`waitlist_admin_dbupdater`** — elevated privileges (PROCESS, REPLICATION CLIENT, KILL). The REST API uses `waitlist_admin_dbappuser` (SELECT/EXECUTE only). Dashboard, backup, and migration ops all use `waitlist_admin_dbupdater` |
| 2 | How granular should table stats be? | **Dynamic — query `information_schema.TABLES`** filtered to `mtm_waitlist`. Adapts as new tables are added via migrations |
| 3 | Auto-refresh or button-only? | **Auto-refresh every 30 seconds** using `PeriodicTimer`. `SHOW GLOBAL STATUS` is safe to poll at this frequency on MySQL 5.7. `information_schema` queries are filtered with `WHERE table_schema = 'mtm_waitlist'` to minimize overhead. `performance_schema` tables are preferred over `SHOW STATUS` where available in 5.7.6+ |

---

## Overview

The Dashboard is the home screen of the admin app. It gives an IT admin or developer an at-a-glance view of the server health without opening MySQL Workbench. It is not a replacement for a full DBA tool — it surfaces the specific metrics relevant to the Waitlist application.

---

## Dashboard Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  ● API Running on :5000   ● DB Connected   3 Clients Active     │
├──────────────┬──────────────┬──────────────┬────────────────────┤
│  UPTIME      │  DB SIZE     │  CONNECTIONS │  WAIT REQUESTS     │
│  2h 14m      │  14.2 MB     │  3 / 100     │  47 total today    │
│  since 08:00 │  mtm_waitlist│  active/max  │  2 open, 1 overdue │
├──────────────┴──────────────┴──────────────┴────────────────────┤
│  TABLE STATUS                                                   │
│  ┌────────────────────┬───────────┬────────────┬─────────────┐  │
│  │ Table              │ Rows      │ Data Size  │ Last Update │  │
│  ├────────────────────┼───────────┼────────────┼─────────────┤  │
│  │ WaitlistEntries    │ 47        │ 32 KB      │ 10:14:22    │  │
│  │ Users              │ 12        │ 8 KB       │ 09:31:05    │  │
│  │ RefreshTokens      │ 3         │ 4 KB       │ 10:13:58    │  │
│  │ SharedWorkstations │ 2         │ 2 KB       │ 08:02:11    │  │
│  └────────────────────┴───────────┴────────────┴─────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│  ACTIVE CONNECTIONS                                             │
│  ┌────────┬──────────┬──────────────┬────────────┬───────────┐  │
│  │ Thread │ User     │ Host         │ Command    │ Time (s)  │  │
│  ├────────┼──────────┼──────────────┼────────────┼───────────┤  │
│  │ 42     │ waitlist │ 10.0.1.15   │ Sleep      │ 12        │  │
│  │ 43     │ waitlist │ 10.0.1.22   │ Query      │ 0         │  │
│  └────────┴──────────┴──────────────┴────────────┴───────────┘  │
├─────────────────────────────────────────────────────────────────┤
│  RECENT ACTIVITY LOG (last 20 lines)                            │
│  10:14:23  [API] POST /api/waitlist — 201 Created              │
│  10:14:12  [API] GET  /api/waitlist — 200 OK (12 entries)      │
│  10:13:58  [API] POST /api/auth/refresh — 200 OK               │
│  ...                                                             │
│                              [↓ Open Full Log]  [⟳ Refresh]    │
└─────────────────────────────────────────────────────────────────┘
```

---

## Data Sources

### Summary Cards

| Card | MySQL Query |
|---|---|
| API Uptime | Calculated from in-process `DateTime.UtcNow - _startedAt` (no query needed) |
| DB Size | `SELECT SUM(data_length + index_length) FROM information_schema.TABLES WHERE table_schema = 'mtm_waitlist'` |
| Active Connections | `SHOW GLOBAL STATUS LIKE 'Threads_connected'` + `SHOW VARIABLES LIKE 'max_connections'` |
| Wait Requests today | `SELECT COUNT(*), SUM(CASE WHEN Status NOT IN ('Completed','Cancelled') THEN 1 END) FROM WaitlistEntries WHERE DATE(RequestedAt) = CURDATE()` |

### Table Status Grid

```sql
SELECT
    table_name       AS TableName,
    table_rows       AS EstimatedRows,
    data_length      AS DataBytes,
    update_time      AS LastUpdated
FROM information_schema.TABLES
WHERE table_schema = 'mtm_waitlist'
ORDER BY table_name;
```

Note: `table_rows` in InnoDB is an estimate. For exact counts on small tables, the DAO can optionally run `SELECT COUNT(*)` on each — only do this when the user explicitly clicks "Refresh Exact".

### Active Connections Grid

```sql
SHOW FULL PROCESSLIST;
```

Filter to show only non-`system user` processes. Display: Thread ID, User, Host (IP only — strip port), Command, Time in seconds, State (first 40 chars if Query/Locked).

Rows are **grouped by `User`** in the dashboard UI. The user row shows a summary (user name + thread count) and expands to reveal individual thread rows. Kill buttons only appear on expanded thread rows.

### Recent Activity Log

This is an **in-process ring buffer** — the ASP.NET middleware logs the last 200 request/response events to a `ConcurrentQueue<LogEntry>` in memory. The dashboard reads from this queue. It is NOT a database query.

Each `LogEntry`:
```csharp
public record LogEntry(DateTime TimestampUtc, string Method, string Path, int StatusCode, long DurationMs);
```

Middleware registration in the API:
```csharp
app.Use(async (ctx, next) =>
{
    var sw = Stopwatch.StartNew();
    await next();
    sw.Stop();
    _logBuffer.Enqueue(new LogEntry(DateTime.UtcNow, ctx.Request.Method,
        ctx.Request.Path, ctx.Response.StatusCode, sw.ElapsedMilliseconds));
    if (_logBuffer.Count > 200) _logBuffer.TryDequeue(out _);
});
```

---

## Auto-Refresh

The dashboard VM polls on a 30-second timer using `PeriodicTimer`:

```csharp
[RelayCommand]
private async Task StartAutoRefreshAsync(CancellationToken ct)
{
    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
    while (await timer.WaitForNextTickAsync(ct))
    {
        await RefreshAsync();
    }
}
```

The `CancellationToken` is cancelled when the view is navigated away from (tied to page `Unloaded` event).

A "Refresh" button triggers `RefreshAsync()` immediately without waiting for the next tick.

---

## API Status Indicator

The top status bar reads directly from the in-process `IHostApplicationLifetime`:

```csharp
// ViewModel_Shell — always visible in the nav bar
[ObservableProperty] private string _apiStatus = "Starting...";
[ObservableProperty] private bool _isApiRunning;

// Injected from ASP.NET's DI container
private readonly IHostApplicationLifetime _lifetime;
```

When `_lifetime.ApplicationStarted` fires → `IsApiRunning = true`, `ApiStatus = $"Running on :{_port}"`.

---

## ViewModel

```
Module.Dashboard/
  ViewModels/
    ViewModel_Dashboard.cs
  Views/
    View_Dashboard.xaml
    View_Dashboard.xaml.cs
```

Key observable properties:
```csharp
[ObservableProperty] TimeSpan _apiUptime
[ObservableProperty] string _dbSizeMb
[ObservableProperty] int _activeConnectionCount
[ObservableProperty] int _maxConnections
[ObservableProperty] int _totalRequestsToday
[ObservableProperty] int _openRequestCount
[ObservableProperty] int _overdueRequestCount
[ObservableProperty] ObservableCollection<Model_TableStat> _tableStats
[ObservableProperty] ObservableCollection<Model_ActiveConnection> _activeConnections
[ObservableProperty] ObservableCollection<Model_ConnectionGroup> _groupedConnections  // for the UI grouped view
[ObservableProperty] ObservableCollection<LogEntry> _recentActivity
[ObservableProperty] bool _isRefreshing
[ObservableProperty] DateTime _lastRefreshedAt
```

The dashboard triggers `RefreshCommand` automatically when the page loads (via `View_Dashboard.xaml.cs` `Loaded` event), so the user sees live data immediately without pressing the Refresh button.

Key commands:
```csharp
[RelayCommand] RefreshAsync()
[RelayCommand] StartAutoRefreshAsync(CancellationToken ct)
[RelayCommand] KillConnectionAsync(Model_ActiveConnection connection)  // quick action from grid
[RelayCommand] OpenFullLogAsync()
```

---

## New Core Models

```
Core/Models/Dashboard/
  Model_TableStat.cs         ← TableName, EstimatedRows, DataBytes, LastUpdated
  Model_ActiveConnection.cs  ← ThreadId, User, Host, Command, TimeSeconds, State, IsCritical
                                + CriticalUsers (static set), DetectCritical(), CanKill, KillTooltip
  Model_ConnectionGroup.cs   ← User, Threads (List<Model_ActiveConnection>), ThreadCount
                                + FromConnections() factory — groups and sorts by user
```

---

## `KillConnection` from Dashboard

The **Kill** button in each expanded thread row calls `KILL <thread_id>` directly on MySQL. This is a MySQL-level kill (terminates the query/connection) — not the same as the app-level client kill switch in DATABASE-05. Both are needed:

- **MySQL KILL** (this feature): terminates a hung query or orphaned connection at the database level. Used by IT to clean up stale connections.
- **App-level kill switch** (DATABASE-05): sends a graceful-shutdown command to the MAUI client application. The app warns the user and closes cleanly.

### Critical Connection Protection

Some MySQL connections belong to the admin app's own internal service accounts. Killing these connections crashes the app. The following safeguards are in place:

1. **`Model_ActiveConnection.IsCritical`** — set to `true` at query time using `Model_ActiveConnection.DetectCritical(user, host)`.
2. **Kill button disabled** — the XAML Kill button is bound to `CanKill` (inverse of `IsCritical`) with a tooltip explaining why it is disabled.
3. **ViewModel guard** — `KillConnectionAsync` returns early if `connection.IsCritical` is `true`.
4. **Service guard** — `Service_Dashboard.KillConnectionAsync` re-queries the processlist before issuing `KILL` and throws `InvalidOperationException` if the thread is critical, preventing bypass through any code path.

Connections are considered critical if `User` is `waitlist_admin_dbupdater` or `waitlist_admin_dbappuser`, or if `Host` is `localhost`, `127.0.0.1`, or `::1`.

---

## New MySQL Users Required

The admin app requires two dedicated MySQL users. Add to `Database/schema/admin/Admin_Users.sql`:

```sql
-- Creates the two MySQL users for the Server Admin application.
-- Run once manually by a root-level MySQL user on the server.
-- Store passwords in server-settings.json (never in source control).

-- App user: used by the REST API for all client-facing requests.
-- Minimal privileges: EXECUTE (stored procedures) + SELECT on the app tables.
CREATE USER IF NOT EXISTS 'waitlist_admin_dbappuser'@'localhost'
    IDENTIFIED BY '*** SET STRONG PASSWORD HERE ***';

GRANT EXECUTE, SELECT
    ON `mtm_waitlist`.* TO 'waitlist_admin_dbappuser'@'localhost';

-- Updater user: used by the admin app for dashboard, backup, and migrations.
-- Elevated privileges required for monitoring and maintenance operations.
CREATE USER IF NOT EXISTS 'waitlist_admin_dbupdater'@'localhost'
    IDENTIFIED BY '*** SET STRONG PASSWORD HERE ***';

GRANT SELECT, PROCESS, REPLICATION CLIENT, KILL
    ON *.* TO 'waitlist_admin_dbupdater'@'localhost';

GRANT ALL PRIVILEGES
    ON `mtm_waitlist`.* TO 'waitlist_admin_dbupdater'@'localhost';

FLUSH PRIVILEGES;
```

`REPLICATION CLIENT` is needed for `SHOW MASTER STATUS` (useful in backup — DATABASE-04).  
`PROCESS` is needed for `SHOW FULL PROCESSLIST`.  
`KILL` is needed for session termination from the dashboard.

---

## Open Decisions

- **Exact row counts vs. estimates:** `information_schema.TABLES.table_rows` is InnoDB's estimate (can be significantly off on large tables). For the small tables in this app, `COUNT(*)` is fine. For future large tables (history, audit log), decide whether to show the estimate with a disclaimer or always run exact counts.
- **Log file persistence:** The in-process ring buffer is lost when the admin app restarts. Should the last N log entries be persisted to a local file? For v1, no — the buffer is ephemeral. A future enhancement would write to a rolling log file.
- **MySQL error monitoring:** Should the dashboard show recent MySQL error log entries? This requires reading the MySQL error log file path from `SHOW VARIABLES LIKE 'log_error'` and tailing the file. Useful but non-trivial — defer to v2.
