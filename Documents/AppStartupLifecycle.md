# App Startup Lifecycle

## Purpose

This document records the current startup behavior for the MTM Waitlist client app from process launch until the authenticated main window is idle. It also records the startup edge cases that affect reliability and the fixes applied for the current lifecycle design.

## Current startup sequence

### WinUI host

1. Windows starts the standalone WinUI process.
2. `App` is constructed.
3. `App.InitializeComponent()` loads WinUI application resources.
4. `App.ConfigureServices()` loads embedded configuration.
5. Debug logging is registered in Debug builds.
6. HTTP, data, repository, service, ViewModel, page, and lifecycle dependencies are registered.
7. The root service provider is built.
8. `App.OnLaunched()` creates `MainWindow`.
9. `MainWindow.InitializeComponent()` loads the shell XAML.
10. `MainWindow` sizes the native window to `1280x800`.
11. `MainWindow` resolves `LoginPage` from dependency injection.
12. `LoginPage` resolves `ViewModel_Auth_Login`.
13. `LoginPage` registers WinUI converters.
14. `LoginPage.InitializeComponent()` loads login XAML.
15. `MainWindow` places `LoginPage` into `LoginFrame`.
16. `App.OnLaunched()` activates the window.
17. `LoginPage.Loaded` executes `ViewModel_Auth_Login.InitializeCommand`.
18. On Windows, the login ViewModel reads the current Windows identity.
19. The login ViewModel checks workstation login mode through the auth service.
20. If the workstation is personal, the login ViewModel attempts auto-login.
21. If manual login is required, the user enters credentials and runs `LoginCommand`.
22. On successful authentication, `LoginPage` raises `Authenticated`.
23. `MainWindow.OnAuthenticated()` hides the login overlay.
24. `MainWindow.OnAuthenticated()` shows the `NavigationView` shell.
25. `MainWindow.OnAuthenticated()` navigates `ShellFrame` to `DashboardPage`.
26. `DashboardPage` resolves `ViewModel_Dashboard_Main`.
27. `DashboardPage.InitializeComponent()` loads dashboard XAML.
28. `MainWindow` starts `IService_AppLifecycle` after Dashboard navigation completes.
29. `Service_AppLifecycle_WinUI` reads the stored authenticated username and display name.
30. `Service_AppLifecycle_WinUI` starts the kill-switch heartbeat.
31. The kill-switch heartbeat posts immediately to `/api/admin/heartbeat`.
32. The kill-switch service polls `/api/admin/shutdown-signal`.
33. The main window becomes idle on the Dashboard.

### Droid host

1. Android starts `MainActivity` using the MAUI splash theme.
2. `MauiProgram.CreateMauiApp()` writes an Android startup diagnostic message.
3. `MauiProgram.CreateMauiApp()` creates a MAUI app builder.
4. `UseSharedMauiApp()` configures the shared MAUI app.
5. The shared startup registers fonts.
6. Debug logging is registered in Debug builds.
7. Embedded configuration is loaded.
8. HTTP, data, repository, service, ViewModel, page, and lifecycle dependencies are registered.
9. The MAUI app is built.
10. `LocalDbContext` is initialized before early background services use SQLite.
11. `SyncService` is eagerly resolved so connectivity monitoring starts.
12. In Debug builds, mock-data seeding may run if the primary API is unreachable.
13. MAUI constructs `App` from dependency injection.
14. `App.InitializeComponent()` loads app resources.
15. `App.CreateWindow()` resolves `View_Auth_Login`.
16. `App.CreateWindow()` creates a window containing the login page.
17. `App.CreateWindow()` starts stored-session validation in the background.
18. If the stored token is valid, `App` swaps directly to `AppShell`.
19. If no valid stored token exists, the login screen remains visible.
20. On successful login, `App.OnAuthenticated()` swaps to `AppShell`.
21. `AppShell` receives the Dashboard page as its first shell content.
22. The shared `App` starts `IService_AppLifecycle` after shell activation.
23. `Service_AppLifecycle_Droid` reads the stored authenticated username and display name.
24. `Service_AppLifecycle_Droid` starts the kill-switch heartbeat.
25. The kill-switch heartbeat posts immediately to `/api/admin/heartbeat`.
26. The kill-switch service polls `/api/admin/shutdown-signal`.
27. The app becomes idle on the Dashboard.

## Startup edge cases

### Authentication and session state

| Edge case | Impact | Fix |
|---|---|---|
| Stored token exists but is expired | App must show login instead of shell | Stored-session validation checks the expiry timestamp. |
| Stored token exists but username/display name are missing | Kill-switch heartbeat cannot identify the user | Lifecycle service validates identity before starting heartbeat. |
| Auto-login and stored-session checks complete at similar times | Shell may be swapped twice | Existing MAUI path uses an interlocked guard; WinUI lifecycle start is idempotent. |
| Authentication succeeds but lifecycle startup fails | User reaches dashboard but server does not see the client | Lifecycle services log failures and start kill-switch only after identity is available. |

### Kill switch and connected clients

| Edge case | Impact | Fix |
|---|---|---|
| WinUI host does not register `IService_KillSwitch` | Admin kill-switch module never sees WinUI clients | Implemented: WinUI service registration includes kill-switch and lifecycle dependencies. |
| WinUI host authenticates but never calls `StartHeartbeat` | Admin kill-switch module never shows this app as initializing or connected | Implemented: WinUI lifecycle service starts after authenticated shell navigation. |
| Droid startup reaches shell through stored session | Heartbeat must start without manual login | Implemented: Droid lifecycle service starts after stored-session shell swap. |
| Heartbeat starts before JWT is stored | Heartbeat returns unauthorized | Lifecycle services run only after successful auth or stored-session validation. |
| Heartbeat loop is started twice | Duplicate polling and duplicate events | `Service_KillSwitch.StartHeartbeat` is guarded; lifecycle services are also idempotent. |
| Server returns no shutdown signal | Client should stay alive | Client treats no-content/no-data as no active signal. |
| Server is unreachable | Client should remain usable where possible | Kill-switch service logs heartbeat/poll failures and retries on the next interval. |

### Startup infrastructure

| Edge case | Impact | Fix |
|---|---|---|
| Configuration resource missing | App fails before first screen | Startup docs identify embedded config as required. |
| Local database is used before initialization | Sync or debug seeding can fail | Shared MAUI startup initializes `LocalDbContext` before early sync/seeding. |
| Debug mock seeding masks server problems | Developers may confuse mock data with live data | Debug seeding remains background-only and logged. |
| Android activity is relaunched while app is active | Duplicate lifecycle work could occur | Android uses single-top launch and lifecycle start is idempotent. |
| WinUI window is activated before page graph is ready | Blank or partial startup UI | WinUI constructs login page before window activation completes. |

## Current kill-switch startup contract

The client appears in the server admin kill-switch UI only after the client posts to `/api/admin/heartbeat`. Registration in dependency injection is not enough. The authenticated lifecycle service must call `IService_KillSwitch.StartHeartbeat(username, fullName, workstationName)` after a valid token is stored and before the app is considered idle.

## Platform lifecycle service split

- `IService_AppLifecycle` is the shared contract for platform startup lifecycle orchestration.
- `Service_AppLifecycle_WinUI` is registered by the standalone WinUI host and owns Windows-host lifecycle behavior.
- `Service_AppLifecycle_Droid` is registered by the shared MAUI app only for Android and owns Droid lifecycle behavior.
- Both services start after authenticated shell activation.
- Both services read identity from secure storage.
- Both services call `IService_KillSwitch.StartHeartbeat(...)`.
- Both services are idempotent and authenticated-session based.

## Implemented startup lifecycle pattern

Both WinUI and Droid now follow the same lifecycle pattern:

1. Host constructs the app.
2. Host builds or receives dependency injection.
3. Login UI is shown first.
4. Stored-session validation or manual/auto-login establishes authentication.
5. The authenticated shell is activated.
6. The platform `IService_AppLifecycle` implementation is started.
7. The lifecycle service reads secure identity.
8. The lifecycle service starts kill-switch heartbeat.
9. The first heartbeat immediately registers the client with the server admin app.
10. The app is idle on Dashboard while background lifecycle services continue.
