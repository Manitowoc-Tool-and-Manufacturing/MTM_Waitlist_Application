---
applyTo: "**/*.{cs,xaml,csproj}"
---

# Codebase State — MTM Waitlist Application
*Verified against source files on May 10, 2026. Do not assume — read this first.*

## What Is Actually Built

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
Feature.Dashboard: ViewModel_Dashboard_Main (Transient)
                   View_Dashboard_Main (Transient)
Feature.Waitlist:  (commented out — no classes yet)
Feature.Mobile:    (commented out — no classes yet)
```

### AppShell — Current Routes
- `Dashboard` → `View_Dashboard_Main` via `ShellContent ContentTemplate`
- No other routes yet

### API Backend Status
- Backend REST API exists at `http://172.16.1.104:5000` (internal LAN)
- Endpoint for waitlist: `/api/waitlist` (GET, POST, PUT, DELETE)
- Auth endpoints: `/api/auth/login`, `/api/auth/refresh`
- API schema IS finalized — confirmed May 10, 2026 (see `.github/assumptions/05102026-1000AM-Assumptions.md`)
- Auth also adds: `/api/auth/auto-login` (Windows username lookup), `/api/auth/check-workstation` (shared workstation check)

### Database Status
- MySQL 5.7 at `172.16.1.104` — database name: `mtm_waitlist` (lowercase)
- Schema fully defined in `database/migrations/V001__Initial_Schema.sql`
- Tables: `Users`, `SharedWorkstations`, `RefreshTokens`, `WaitlistEntries`
- Auth procedure set includes: `usp_Auth_CheckSharedWorkstation`, `usp_Auth_GetUserByWindowsUsername`, `usp_Auth_ValidateCredentials`, and 5 more
- **NOT YET APPLIED TO SERVER** — awaiting admin credentials and connectivity confirmation

## Key Namespace Patterns

| File location | Namespace |
|--------------|-----------|
| `Feature.Dashboard/ViewModels/Main/` | `MTM_Waitlist_Application.Feature.Dashboard.ViewModels.Main` |
| `Feature.Dashboard/Views/Main/` | `MTM_Waitlist_Application.Feature.Dashboard.Views.Main` |
| `Feature.Waitlist/ViewModels/<Screen>/` | `MTM_Waitlist_Application.Feature.Waitlist.ViewModels.<Screen>` |
| `Feature.Waitlist/Views/<Screen>/` | `MTM_Waitlist_Application.Feature.Waitlist.Views.<Screen>` |
| `Services/Waitlist/` | `MTM_Waitlist_Application.Services.Waitlist` |
| `Services/Auth/` | `MTM_Waitlist_Application.Services.Auth` |
| `Services/Sync/` | `MTM_Waitlist_Application.Services.Sync` |
| `Data/Repositories/Waitlist/` | `MTM_Waitlist_Application.Data.Repositories.Waitlist` |
| `Data/Local/` | `MTM_Waitlist_Application.Data.Local` |
| `Data/Http/` | `MTM_Waitlist_Application.Data.Http` |
| `Core/Models/Waitlist/` | `MTM_Waitlist_Application.Core.Models.Waitlist` |
| `Core/Models/Auth/` | `MTM_Waitlist_Application.Core.Models.Auth` |
| `Core/Models/Shared/` | `MTM_Waitlist_Application.Core.Models.Shared` |
| `Core/Interfaces/Waitlist/` | `MTM_Waitlist_Application.Core.Interfaces.Waitlist` |
| `Core/Interfaces/Auth/` | `MTM_Waitlist_Application.Core.Interfaces.Auth` |
| `Core/Interfaces/Api/` | `MTM_Waitlist_Application.Core.Interfaces.Api` |
| `Core/Interfaces/Sync/` | `MTM_Waitlist_Application.Core.Interfaces.Sync` |
| `Core/Constants/Api/` | `MTM_Waitlist_Application.Core.Constants.Api` |
| `Core/Enums/Waitlist/` | `MTM_Waitlist_Application.Core.Enums.Waitlist` |
| `Core/Enums/Auth/` | `MTM_Waitlist_Application.Core.Enums.Auth` |

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
- Token stored via `SecureStorage.SetAsync("auth_token", token)`
- `HttpApiClient` reads token from SecureStorage and attaches as Bearer header on every request
- `Service_Auth` handles login, logout, refresh — uses `/api/auth/login` and `/api/auth/refresh`

## Offline Queue Pattern
- `Entity_OfflineWriteQueue` table tracks pending writes (INSERT / UPDATE / DELETE)
- `Service_WaitlistEntry` enqueues writes when offline
- `SyncService` flushes queue on `ConnectivityChanged` when NetworkAccess == Internet
- `SyncService` uses `SemaphoreSlim(1,1)` to prevent concurrent flushes
- `SyncService` constructor subscribes immediately — eagerly resolved at startup
