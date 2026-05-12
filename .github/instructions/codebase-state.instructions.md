---
applyTo: "**/*.{cs,xaml,csproj}"
---

# Codebase State — MTM Waitlist Application
*Verified against source files on May 12, 2026. Do not assume — read this first.*

## What Is Actually Built

### Feature.Auth — IN PROGRESS
- `ViewModels/Login/ViewModel_Auth_Login.cs` — `partial class`, inherits `ObservableObject`
- `Views/Login/View_Auth_Login.Windows.xaml` — centered desktop login card
- `Views/Login/View_Auth_Login.Android.xaml` — full-screen mobile login layout
- `Views/Login/View_Auth_Login.xaml.cs` — shared code-behind, init + authenticated handoff event only
- Supports manual username/password login through `IService_Auth.LoginAsync`
- Supports workstation detection and silent Windows auto-login through `IService_Auth.CheckWorkstationAsync()` and `IService_Auth.AutoLoginAsync()`
- Stores access token, refresh token, expiry, and role in secure storage
- App startup bypasses the login screen when a stored token is still valid

### Feature.Dashboard — COMPLETE
- `ViewModels/Main/ViewModel_Dashboard_Main.cs` — `partial class`, inherits `ObservableObject`
- `Views/Main/View_Dashboard_Main.Windows.xaml` — multi-column Grid, 4-card summary row
- `Views/Main/View_Dashboard_Main.Android.xaml` — single-column ScrollView, card layout
- `Views/Main/View_Dashboard_Main.xaml.cs` — shared code-behind, DI ctor binding only
- `x:DataType="vm:ViewModel_Dashboard_Main"` on both XAML files
- Observable properties: `IsBusy`, `StatusMessage` — no real service calls yet (placeholder data)

### Feature.Waitlist — EMPTY STUB
- Only `Platforms/` folder exists with empty Android/iOS/MacCatalyst/Windows subfolders
- No Views, no ViewModels, no .csproj CommunityToolkit.Mvvm package, no MVVMTK0045 suppression yet

### Feature.Mobile — EMPTY STUB
- Only `Platforms/` folder exists with empty Android/iOS/MacCatalyst/Windows subfolders
- No Views, no ViewModels

### Model_WaitlistEntry — FULLY DEFINED (confirmed May 10, 2026)
- All business fields added: `WorkcenterName`, `RequestType` (Enum_WaitlistRequestType), `Status` (Enum_WaitlistStatus), `Priority`, `Notes`, `RequestedAt`, `ScheduledAt`, `CompletedAt`, `AssignedToUserId`, `CreatedByUserId`, `UpdatedByUserId`
- `Entity_WaitlistEntry` (SQLite) mirrors all fields — both files updated
- Enums created: `Enum_WaitlistStatus`, `Enum_WaitlistRequestType` (Core/Enums/Waitlist/)
- `Enum_UserRole` created (Core/Enums/Auth/)
- `Model_SharedWorkstation` created (Core/Models/Auth/)

### DI — Current Registrations in AddSharedServices()
```
Infrastructure:  IConnectivity (Singleton — Connectivity.Current)
Data:            LocalDbContext (Singleton)
                 IApiClient → HttpApiClient (Singleton)
                 IRepository_WaitlistEntry → Repository_WaitlistEntry (Singleton)
                 IRepository_WaitlistEntryLocal → Repository_WaitlistEntryLocal (Singleton)
Services:        IService_Auth → Service_Auth (Singleton)
                 IService_WaitlistEntry → Service_WaitlistEntry (Singleton)
                 ISyncService → SyncService (Singleton)
Feature.Auth:     ViewModel_Auth_Login (Transient)
                  View_Auth_Login (Transient)
Feature.Dashboard: ViewModel_Dashboard_Main (Transient)
                   View_Dashboard_Main (Transient)
Feature.Waitlist:  (commented out — no classes yet)
Feature.Mobile:    (commented out — no classes yet)
```

### Startup Flow — Current
- `App.CreateWindow()` opens `AppShell` immediately when a stored token is still valid
- Otherwise startup resolves `View_Auth_Login` from DI
- `ViewModel_Auth_Login.InitializeAsync()` checks workstation mode and attempts Windows auto-login on personal machines
- Successful login or auto-login switches the window page to `AppShell`

### AppShell — Current Routes
- `Dashboard` → `View_Dashboard_Main` via `ShellContent ContentTemplate`
- No other routes yet

### API Backend Status
- Backend REST API is hosted by the separate `MTM_Waitlist_Server/` solution in this repository
- Server app exists and is implemented as a separate standalone solution (`MTM_Waitlist_Server.slnx`)
- Runtime base URL remains `http://172.16.1.104:5000` (internal LAN)
- Health endpoint: `/health` (GET/HEAD)
- Endpoint for waitlist: `/api/waitlist` (GET, POST, PUT, DELETE)
- Auth endpoints: `/api/auth/login`, `/api/auth/refresh`, `/api/auth/revoke`, `/api/auth/auto-login`, `/api/auth/check-workstation`

### Database Status
- MySQL 5.7 at `172.16.1.104` — database name: `mtm_waitlist` (lowercase)
- Server-side database implementation now lives under `MTM_Waitlist_Server/Database/`
- Client repo `Database/` folder remains the shared reference copy used for client-side documentation and schema visibility
- Tables: `Users`, `SharedWorkstations`, `RefreshTokens`, `WaitlistEntries`
- Auth procedure set includes: `usp_Auth_CheckSharedWorkstation`, `usp_Auth_GetUserByWindowsUsername`, `usp_Auth_ValidateCredentials`, and 5 more
- Database design docs have been implemented in the server solution; treat them as built server-side unless a specific source file says otherwise

## Key Namespace Patterns

| File location | Namespace |
|--------------|-----------|
| `Feature.Dashboard/ViewModels/Main/` | `Feature.Dashboard.ViewModels.Main` |
| `Feature.Dashboard/Views/Main/` | `Feature.Dashboard.Views.Main` |
| `Feature.Auth/ViewModels/Login/` | `Feature.Auth.ViewModels.Login` |
| `Feature.Auth/Views/Login/` | `Feature.Auth.Views.Login` |
| `Feature.Waitlist/ViewModels/<Screen>/` | `Feature.Waitlist.ViewModels.<Screen>` |
| `Feature.Waitlist/Views/<Screen>/` | `Feature.Waitlist.Views.<Screen>` |
| `Services/Waitlist/` | `Services.Waitlist` |
| `Services/Auth/` | `Services.Auth` |
| `Services/Sync/` | `Services.Sync` |
| `Data/Repositories/Waitlist/` | `Data.Repositories.Waitlist` |
| `Data/Local/` | `Data.Local` |
| `Data/Http/` | `Data.Http` |
| `Core/Models/Waitlist/` | `Core.Models.Waitlist` |
| `Core/Models/Auth/` | `Core.Models.Auth` |
| `Core/Models/Shared/` | `Core.Models.Shared` |
| `Core/Interfaces/Waitlist/` | `Core.Interfaces.Waitlist` |
| `Core/Interfaces/Auth/` | `Core.Interfaces.Auth` |
| `Core/Interfaces/Api/` | `Core.Interfaces.Api` |
| `Core/Interfaces/Sync/` | `Core.Interfaces.Sync` |
| `Core/Constants/Api/` | `Core.Constants.Api` |
| `Core/Enums/Waitlist/` | `Core.Enums.Waitlist` |
| `Core/Enums/Auth/` | `Core.Enums.Auth` |
| `Tests/Unit/Core.Tests/<Folder>/<Subfolder>/<Category>/` | `MTM_Waitlist_Application.Tests.Unit.Core.<Folder>.<Subfolder>.<Category>` |
| `Tests/Unit/Data.Tests/<Folder>/<Subfolder>/<Category>/` | `MTM_Waitlist_Application.Tests.Unit.Data.<Folder>.<Subfolder>.<Category>` |
| `Tests/Unit/Services.Tests/<Folder>/<Subfolder>/<Category>/` | `MTM_Waitlist_Application.Tests.Unit.Services.<Folder>.<Subfolder>.<Category>` |
| `Tests/Unit/Feature.Dashboard.Tests/<Folder>/<Subfolder>/<Category>/` | `MTM_Waitlist_Application.Tests.Unit.Feature.Dashboard.<Folder>.<Subfolder>.<Category>` |
| `Tests/Unit/Feature.Waitlist.Tests/<Folder>/<Subfolder>/<Category>/` | `MTM_Waitlist_Application.Tests.Unit.Feature.Waitlist.<Folder>.<Subfolder>.<Category>` |

## Test Project State (as of May 12, 2026)

| Project | TFM | References | Status |
|---------|-----|-----------|--------|
| `Core.Tests` | `net10.0` | `Core` | ✅ Implemented core model/result tests |
| `Data.Tests` | `net10.0-windows10.0.19041.0` | `Core`, `Data` | ✅ Implemented repository and mock-seed tests |
| `Services.Tests` | `net10.0-windows10.0.19041.0` | `Core`, `Services` | ✅ Implemented auth, waitlist, and sync tests |
| `Feature.Auth.Tests` | `net10.0-windows10.0.19041.0` | `Core`, `Feature.Auth` | ✅ Implemented login ViewModel tests |
| `Feature.Dashboard.Tests` | `net10.0-windows10.0.19041.0` | `Core`, `Feature.Dashboard` | ✅ Implemented ViewModel property tests |
| `Feature.Waitlist.Tests` | `net10.0-windows10.0.19041.0` | `Core`, `Feature.Waitlist` | 🚧 Project exists but no source logic yet |
| `UITests.WinUI` | `net10.0-windows10.0.19041.0` | None | 🚧 Project exists but no authored UI tests yet |
| `UITests.Droid` | `net10.0` | None | 🚧 Project exists but no authored UI tests yet |

Physical disk location: `MTM_Waitlist_Application/Tests/Unit/` and `MTM_Waitlist_Application/Tests/UI/`
Solution folder location: `/Tests/Unit/` and `/Tests/UI/`
See `testing.instructions.md` for folder structure rules and category definitions.

## Mock Data / Debug Seeding
- `MockDataSeeder` in `Data/Mock/MockDataSeeder.cs` (only compiled in `#if DEBUG`)
- Seeds 5 entries with IDs 9001–9005 when primary API unreachable AND local DB empty
- Runs on background thread, 1-second delay after app start
- Seed IDs start at 9001 to avoid colliding with real API IDs

## Configuration
- `appsettings.json`: `Api:PrimaryBaseUrl = http://172.16.1.104:5000`, `Api:FallbackBaseUrl = http://localhost:5000`
- `appsettings.Development.json`: same values (currently identical — overrides in Debug)
- Both are embedded resources loaded from assembly manifest in `UseSharedMauiApp()`

## JWT / Auth Storage
- Access token stored via `SecureStorage.SetAsync("auth_token", token)`
- Refresh token stored via `SecureStorage.SetAsync("refresh_token", refreshToken)`
- Expiry stored via `SecureStorage.SetAsync("auth_token_expires_at", expiresAt)`
- Role stored via `SecureStorage.SetAsync("auth_role", role)`
- `HttpApiClient` reads token from SecureStorage and attaches as Bearer header on every request
- `Service_Auth` handles login, auto-login, workstation detection, logout, and refresh

## Offline Queue Pattern
- `Entity_OfflineWriteQueue` table tracks pending writes (INSERT / UPDATE / DELETE)
- `Service_WaitlistEntry` enqueues writes when offline
- `SyncService` flushes queue on `ConnectivityChanged` when NetworkAccess == Internet
- `SyncService` uses `SemaphoreSlim(1,1)` to prevent concurrent flushes
- `SyncService` constructor subscribes immediately — eagerly resolved at startup
