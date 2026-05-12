---
description: "MTM Waitlist Application development guidelines - MVVM architecture, MAUI multi-project patterns, naming conventions, and platform-specific best practices"
applyTo: "**/*.{cs,xaml,csproj,md,txt,ps1,json,yaml,yml,xml,config}"
---

# MTM Waitlist Application — Copilot Instructions

Mobile-companion + Windows-primary waitlist management application built on .NET MAUI.

---

## 🚨 CRITICAL ARCHITECTURE RULES — READ FIRST

### FORBIDDEN — These Will Break the System

**❌ NEVER DO THESE:**

1. **ViewModels referencing Data layer directly** — MUST go through Service layer
2. **Host projects (.WinUI / .Droid) referencing anything except Shared** — thin launchers only
3. **Feature projects referencing Data project** — repositories come via DI only
4. **Feature projects referencing other Feature projects** — cross-feature via Services only
5. **Business logic in `MauiProgram.cs`** — belongs in `MauiProgramExtensions.cs` or Services
6. **Business logic in `MainActivity.cs`** — Android lifecycle only
7. **Runtime `{Binding}` in XAML** — use compile-time `x:DataType` + compiled bindings only
8. **`new` keyword to instantiate services, repositories, or ViewModels** — inject via DI always
9. **`Singleton` lifetime for pages or ViewModels** — always `Transient`
10. **`\` for PowerShell line continuation** — use backtick `` ` `` always

### REQUIRED — Every Component Must Follow

**✅ ALWAYS DO THESE:**

1. **MVVM Layer Flow:** View (XAML) → ViewModel → Service → Repository → Database
2. **ViewModels:** Partial classes inheriting from `ObservableObject` (CommunityToolkit.Mvvm)
3. **Services:** Interface-based, registered in `AddSharedServices()`
4. **Repositories:** Instance-based, injected via constructor, return `Model_Dao_Result`
5. **XAML Bindings:** Use `x:DataType` with compiled bindings and explicit `Mode`
6. **Async Methods:** All must end with `Async` suffix
7. **Error Handling:** Repositories return results, Services handle them, ViewModels display them
8. **XML Doc Comments:** All public and internal classes and methods must have `/// <summary>`
9. **Inline Comments:** Explain the *why*, not the *what*
10. **New registrations:** Always add to `AddSharedServices()` in `MauiProgramExtensions.cs`
11. **PowerShell:** Use backtick `` ` `` for line continuation — never `\`
12. **Solution file:** Always target `MTM_Waitlist_Application.slnx` in CLI commands

### 🛑 ASSUMPTION DOCUMENTATION — REQUIRED BEFORE PROCEEDING

Whenever the AI agent is about to make a **major assumption**, it **MUST** first
create an assumption file for the user to review before continuing.

**File location:** `.github/assumptions/`
**File naming:** `MMDDYYYY-HHMMam/pm-Assumptions.md`
**Example:** `05072026-0230PM-Assumptions.md`

**Required contents:**
1. Numbered list of each assumption
2. Why the assumption is needed
3. Potential impact if wrong
4. Alternative interpretations considered
5. Explicit request for user confirmation before proceeding

---

## Technology Stack

- **Framework:** .NET MAUI 10
- **Language:** C# 13
- **Platform:** .NET 10
- **Architecture:** MVVM with CommunityToolkit.Mvvm
- **Solution format:** `.slnx` (VS 2026 — NOT `.sln`)
- **Targets:** Windows (WinUI) primary · Android companion
- **DI Container:** Microsoft.Extensions.DependencyInjection
- **Local storage:** sqlite-net-pcl 1.9.172 + SQLitePCLRaw.bundle_green 2.1.11 (NOT EF Core)

### NuGet Packages Per Project (verified)

| Project | Key Packages |
|---------|-------------|
| Core | _(none — plain `net10.0` SDK)_ |
| Data | `Microsoft.Maui.Controls 10.0.60`, `Microsoft.Extensions.Http 10.0.7`, `sqlite-net-pcl 1.9.172`, `SQLitePCLRaw.bundle_green 2.1.11` |
| Services | `Microsoft.Maui.Controls 10.0.60` (for `IConnectivity`) |
| Feature.* (new) | `Microsoft.Maui.Controls 10.0.60`, `CommunityToolkit.Mvvm 8.4.2` |
| Shared | `Microsoft.Maui.Controls 10.0.60`, `Microsoft.Extensions.Logging.Debug 10.0.7`, `Microsoft.Extensions.Configuration.Json 10.0.7` |
| WinUI / Droid | `Microsoft.Maui.Controls 10.0.60`, `Microsoft.Extensions.Logging.Debug 10.0.7` |

---

## Solution Structure

```
MTM_Waitlist_Application.slnx
├── Hosts/
│   ├── MTM_Waitlist_Application.WinUI     ← thin Windows launcher — Shared reference only
│   └── MTM_Waitlist_Application.Droid     ← thin Android launcher — Shared reference only
├── Shared/
│   └── MTM_Waitlist_Application           ← DI hub, navigation, global styles
│       └── MauiProgramExtensions.cs       ← ALL service registration lives here
├── Core/
│   ├── Core      ← models, interfaces, constants — zero dependencies
│   ├── Services  ← business logic — references Core only
│   └── Data      ← repositories, sqlite-net-pcl — references Core only
└── Features/
    ├── Feature.Waitlist    ← XAML + ViewModels
    ├── Feature.Dashboard   ← XAML + ViewModels
    └── Feature.Mobile      ← Android-only screens
```

---

## Naming Conventions

### Classes

| Type | Convention | Example |
|------|-----------|---------|
| ViewModel | `ViewModel_<Feature>_<Screen>` | `ViewModel_Waitlist_Entry` |
| View (Page) | `View_<Feature>_<Screen>` | `View_Waitlist_Entry` |
| Service interface | `IService_<Purpose>` | `IService_WaitlistEntry` |
| Service implementation | `Service_<Purpose>` | `Service_WaitlistEntry` |
| Repository interface | `IRepository_<Entity>` | `IRepository_WaitlistEntry` |
| Repository implementation | `Repository_<Entity>` | `Repository_WaitlistEntry` |
| Model / entity | `Model_<Entity>` | `Model_WaitlistEntry` |
| Enum | `Enum_<Category>` | `Enum_WaitlistStatus` |
| Helper | `Helper_<Category>_<Function>` | `Helper_Navigation_Shell` |
| Constants class | `Constants_<Category>` | `Constants_AppSettings` |

### Methods

- PascalCase for all methods
- Async methods MUST end with `Async`: `LoadEntriesAsync()`, `SaveAsync()`
- Repository methods: `<Action><Entity>Async` — e.g., `InsertWaitlistEntryAsync()`
- Command methods: `<Action><Target>Async` — e.g., `ApproveEntryAsync()`

### Properties and Fields

- PascalCase for public properties
- `_camelCase` for private fields (underscore prefix)
- Observable properties: `[ObservableProperty]` on `private` field
- Commands: `[RelayCommand]` on `private async Task` method

---

## File Placement Rules

All `.cs` and `.xaml` files **must** be placed in a segregated domain subfolder within their type folder. Files placed directly in a type root (e.g., `Models/Model_X.cs` with no subfolder) are a violation.

**Pattern:** `<TypeFolder>/<DomainSubfolder>/FileName.cs`

The domain subfolder groups files by the feature or concern they belong to — not by the project they live in.

### Core (`Core/`)

| What | Path pattern |
|------|--------------|
| Model / entity | `Models/<Domain>/Model_<Entity>.cs` |
| Repository interface | `Interfaces/<Domain>/IRepository_<Entity>.cs` |
| Service interface | `Interfaces/<Domain>/IService_<Purpose>.cs` |
| Constants | `Constants/<Domain>/Constants_<Category>.cs` |
| Enums | `Enums/<Domain>/Enum_<Category>.cs` |

**Canonical domain subfolder examples:**

| File | Full path |
|------|-----------|
| `Model_WaitlistEntry` | `Models/Waitlist/Model_WaitlistEntry.cs` |
| `Model_AuthToken` | `Models/Auth/Model_AuthToken.cs` |
| `Model_Dao_Result` | `Models/Shared/Model_Dao_Result.cs` |
| `IApiClient` | `Interfaces/Api/IApiClient.cs` |
| `IService_Auth` | `Interfaces/Auth/IService_Auth.cs` |
| `IRepository_WaitlistEntry` | `Interfaces/Waitlist/IRepository_WaitlistEntry.cs` |
| `IService_WaitlistEntry` | `Interfaces/Waitlist/IService_WaitlistEntry.cs` |
| `ISyncService` | `Interfaces/Sync/ISyncService.cs` |
| `Constants_Api` | `Constants/Api/Constants_Api.cs` |

### Data (`Data/`)

| What | Path pattern |
|------|--------------|
| API HTTP client | `Http/HttpApiClient.cs` |
| Local DB context | `Local/LocalDbContext.cs` |
| Repository implementation | `Repositories/<Domain>/Repository_<Entity>.cs` |

**Examples:** `Repositories/Waitlist/Repository_WaitlistEntry.cs` · `Repositories/Waitlist/Repository_WaitlistEntryLocal.cs`

### Services (`Services/`)

| What | Path pattern |
|------|--------------|
| Service implementation | `<Domain>/Service_<Purpose>.cs` |

**Examples:** `Auth/Service_Auth.cs` · `Waitlist/Service_WaitlistEntry.cs` · `Sync/SyncService.cs`

### Features (`MTM_Waitlist_Application.Feature.<Name>/`)

| What | Path pattern |
|------|--------------|
| XAML page (Windows) | `Views/<Screen>/View_<Feature>_<Screen>.Windows.xaml` |
| XAML page (Android) | `Views/<Screen>/View_<Feature>_<Screen>.Android.xaml` |
| Code-behind | `Views/<Screen>/View_<Feature>_<Screen>.xaml.cs` |
| ViewModel | `ViewModels/<Screen>/ViewModel_<Feature>_<Screen>.cs` |

The `<Screen>` subfolder name matches the `<Screen>` segment of the class name (`View_<Feature>_<Screen>`).

**Examples (Feature.Waitlist):** `Views/Entry/View_Waitlist_Entry.Windows.xaml` · `ViewModels/Entry/ViewModel_Waitlist_Entry.cs`

### Shared / Mobile / Resources

| What | Where |
|------|-------|
| Mobile-only pages | `Feature.Mobile/Views/<Screen>/` |
| Global styles / themes | `MTM_Waitlist_Application/Resources/Styles/` |
| Shared images / fonts | `MTM_Waitlist_Application/Resources/` |

---

## Architecture Patterns

### ViewModel Pattern

```csharp
// ✅ CORRECT
namespace Feature.Waitlist.ViewModels;

/// <summary>
/// ViewModel for the waitlist entry screen.
/// Manages display state and user interactions for a single waitlist entry.
/// </summary>
public partial class ViewModel_Waitlist_Entry : ObservableObject
{
    private readonly IService_WaitlistEntry _entryService;

    [ObservableProperty]
    private ObservableCollection<Model_WaitlistEntry> _entries = [];

    [ObservableProperty]
    private Model_WaitlistEntry? _selectedEntry;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ViewModel_Waitlist_Entry(IService_WaitlistEntry entryService)
    {
        _entryService = entryService;
    }

    [RelayCommand]
    private async Task LoadEntriesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Loading...";

        var result = await _entryService.GetAllEntriesAsync();
        if (result.IsSuccess)
        {
            Entries = new ObservableCollection<Model_WaitlistEntry>(result.Data);
            StatusMessage = $"Loaded {Entries.Count} entries";
        }
        else
        {
            StatusMessage = result.ErrorMessage;
        }

        IsBusy = false;
    }
}
```

```csharp
// ❌ FORBIDDEN — ViewModel referencing repository directly
public partial class ViewModel_Bad : ObservableObject
{
    private async Task LoadAsync()
    {
        var repo = new Repository_WaitlistEntry(); // NEVER — use DI
        var result = await repo.GetAllAsync();     // NEVER — go through Service
    }
}
```

### Service Pattern

Services use **two repositories** per entity: one online (API) and one local (SQLite).
Routing between them is based on `IConnectivity.NetworkAccess`. This is the actual
pattern in `Service_WaitlistEntry`.

```csharp
// ✅ CORRECT — connectivity-aware dual-repository service
namespace Services.Waitlist;

public sealed class Service_WaitlistEntry : IService_WaitlistEntry
{
    private readonly IConnectivity _connectivity;
    private readonly IRepository_WaitlistEntry _onlineRepository;
    private readonly IRepository_WaitlistEntryLocal _localRepository;

    public Service_WaitlistEntry(
        IConnectivity connectivity,
        IRepository_WaitlistEntry onlineRepository,
        IRepository_WaitlistEntryLocal localRepository)
    {
        _connectivity = connectivity;
        _onlineRepository = onlineRepository;
        _localRepository = localRepository;
    }

    private bool IsOnline => _connectivity.NetworkAccess == NetworkAccess.Internet;

    public async Task<Model_Dao_Result<List<Model_WaitlistEntry>>> GetAllEntriesAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsOnline)
        {
            var onlineResult = await _onlineRepository.GetAllWaitlistEntriesAsync(cancellationToken);
            if (onlineResult.IsSuccess) { return onlineResult; }
            // Mid-request network failure — fall through to local cache silently.
        }
        return await _localRepository.GetAllWaitlistEntriesAsync();
    }
}
```

### Repository Pattern

There are **two repository types** per entity:
- **Online** (`Repository_<Entity>`) — delegates to `IApiClient` (REST API)
- **Local** (`Repository_<Entity>Local`) — uses `LocalDbContext` (sqlite-net-pcl, NOT EF Core)

```csharp
// ✅ CORRECT — online repository via IApiClient
namespace Data.Repositories.Waitlist;

public sealed class Repository_WaitlistEntry : IRepository_WaitlistEntry
{
    private readonly IApiClient _apiClient;

    public Repository_WaitlistEntry(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<Model_Dao_Result<List<Model_WaitlistEntry>>> GetAllWaitlistEntriesAsync(
        CancellationToken cancellationToken = default)
        => await _apiClient.GetAsync<List<Model_WaitlistEntry>>(
            Constants_Api.WaitlistEntryEndpoint, cancellationToken);
}
```

```csharp
// ✅ CORRECT — local repository via sqlite-net-pcl (LocalDbContext)
namespace Data.Repositories.Waitlist;

public sealed class Repository_WaitlistEntryLocal : IRepository_WaitlistEntryLocal
{
    private readonly LocalDbContext _context;

    public Repository_WaitlistEntryLocal(LocalDbContext context)
    {
        _context = context;
    }

    public async Task<Model_Dao_Result<List<Model_WaitlistEntry>>> GetAllWaitlistEntriesAsync()
    {
        try
        {
            await _context.InitializeAsync();
            var entities = await _context.Connection
                .Table<Entity_WaitlistEntry>().ToListAsync();
            return Model_Dao_Result<List<Model_WaitlistEntry>>.Success(
                entities.Select(MapToModel).ToList());
        }
        catch (Exception ex)
        {
            return Model_Dao_Result<List<Model_WaitlistEntry>>.Failure(
                $"Failed to retrieve local waitlist entries: {ex.Message}");
        }
    }
}
```

> ⚠️ **No EF Core in this project.** Local storage uses `sqlite-net-pcl` with
> `SQLiteAsyncConnection`, `[Table]`, `[PrimaryKey]`, and `[AutoIncrement]` attributes.

### XAML Binding Pattern

```xml
<!-- ✅ CORRECT — compiled bindings with x:DataType -->
<ContentPage
    xmlns:vm="clr-namespace:Feature.Waitlist.ViewModels"
    x:DataType="vm:ViewModel_Waitlist_Entry">

    <CollectionView ItemsSource="{Binding Entries, Mode=OneWay}">
        <CollectionView.ItemTemplate>
            <DataTemplate x:DataType="models:Model_WaitlistEntry">
                <Label Text="{Binding Name}" />
            </DataTemplate>
        </CollectionView.ItemTemplate>
    </CollectionView>

</ContentPage>
```

```xml
<!-- ❌ FORBIDDEN — runtime reflection binding -->
<Label Text="{Binding Name}" />

<!-- ✅ CORRECT — compiled binding -->
<Label Text="{Binding Name, Mode=OneWay}" />
```

### DI Registration (MauiProgramExtensions.cs)

`AddSharedServices()` is an **internal** extension method on `IServiceCollection`.
It is called exclusively by `UseSharedMauiApp()`, which is the **only public entry point**
called by both host `MauiProgram.cs` files. Never call `AddSharedServices()` from host projects.

```csharp
// ✅ CORRECT — all registrations inside AddSharedServices(), called by UseSharedMauiApp()
internal static IServiceCollection AddSharedServices(this IServiceCollection services)
{
    // Repositories — Singleton (stateless, reusable)
    services.AddSingleton<IRepository_WaitlistEntry, Repository_WaitlistEntry>();

    // Services — Singleton (business logic, stateless)
    services.AddSingleton<IService_WaitlistEntry, Service_WaitlistEntry>();

    // ViewModels — Transient (new instance per navigation)
    services.AddTransient<ViewModel_Waitlist_Entry>();

    // Pages — Transient
    services.AddTransient<View_Waitlist_Entry>();

    return services;
}
```

---

## XAML Platform Guidelines

### Windows Layout Rules
- Multi-column `Grid` layouts — use available screen space
- Flyout navigation in `AppShell.xaml` with `FlyoutBehavior` locked on WinUI
- Data-dense `CollectionView` with multiple columns
- `MenuFlyout` for right-click context menus
- Side-by-side `Label` + input control forms
- Minimum button height: 36px

### Android Layout Rules
- Single-column `StackLayout` — one task per screen
- Flyout navigation in `AppShell.xaml` with `FlyoutBehavior` as Flyout on Android
- Card-based single-column `CollectionView`
- `SwipeView` for item actions
- Stacked `Label` above input control forms
- Minimum tap target: **48px height always**

### AppShell Navigation Pattern (verified)
```xml
<Shell FlyoutBehavior="{OnPlatform WinUI=Locked, Android=Flyout}">
    <ShellContent
        Title="Dashboard"
        ContentTemplate="{DataTemplate dashboard:View_Dashboard_Main}"
        Route="Dashboard" />
</Shell>
```

### Separate XAML Per Platform
```
Feature.Waitlist/Views/
├── View_Waitlist_Entry.Windows.xaml    ← rich desktop layout
├── View_Waitlist_Entry.Android.xaml    ← simplified mobile layout
└── View_Waitlist_Entry.xaml.cs         ← shared code-behind (ViewModel binding only)
```

Use `OnIdiom` only for minor property tweaks (font size, padding) — never for
entire layout sections.

---

## Dependency Rules

| Project | ✅ May Reference | ❌ Must NEVER Reference |
|---------|----------------|------------------------|
| `.Droid` | Shared only | Features, Services, Data, Core directly |
| `.WinUI` | Shared only | Features, Services, Data, Core directly |
| `Shared` | All Features, Services, Data, Core | Host projects |
| `Feature.*` | Services, Core | Data, other Features, Shared, Hosts |
| `Services` | Core | Data, Features, Shared, Hosts |
| `Data` | Core | Services, Features, Shared, Hosts |
| `Core` | Nothing | Everything |

---

## Code Quality Standards

### Always Use Braces
```csharp
// ✅ CORRECT
if (condition)
{
    DoSomething();
}

// ❌ FORBIDDEN
if (condition)
    DoSomething();
```

### Explicit Accessibility Modifiers
```csharp
// ✅ CORRECT
private readonly IService_WaitlistEntry _service;
public async Task<Model_Dao_Result> SaveAsync() { }

// ❌ FORBIDDEN
readonly IService_WaitlistEntry _service;
async Task<Model_Dao_Result> SaveAsync() { }
```

### Null Handling
```csharp
// ✅ CORRECT
if (value is null) { return; }
var result = entry?.GetStatus();
public string? OptionalNote { get; set; }

// ❌ AVOID
if (value == null) { return; }
```

### Collection Reloads
```csharp
// ✅ CORRECT — replace instance to avoid excessive UI notifications
Entries = new ObservableCollection<Model_WaitlistEntry>(result.Data);

// ❌ AVOID for full reloads — causes N UI notifications
Entries.Clear();
foreach (var item in result.Data) Entries.Add(item);
```

---

## Debugging Checklist

When debugging, verify:

- [ ] DI registration in `AddSharedServices()` in `MauiProgramExtensions.cs`
- [ ] ViewModel is `partial` class
- [ ] ViewModel inherits from `ObservableObject`
- [ ] XAML uses `x:DataType` and compiled bindings
- [ ] No ViewModel → Repository calls (must go through Service)
- [ ] Repositories return `Model_Dao_Result` (never throw)
- [ ] Async methods end with `Async`
- [ ] No business logic in `MauiProgram.cs` or `MainActivity.cs`
- [ ] Host projects reference Shared only
- [ ] Feature projects do NOT reference Data project
- [ ] New pages and ViewModels are registered as `Transient`
- [ ] New repositories and services are registered as `Singleton`
- [ ] `WMC1006` suppressed in WinUI `.csproj` via `$(NoWarn);WMC1006`
- [ ] `MVVMTK0045` suppressed in any Feature `.csproj` using CommunityToolkit.Mvvm via `$(NoWarn);MVVMTK0045`
- [ ] New Feature `.csproj` includes `CommunityToolkit.Mvvm 8.4.2` package reference
- [ ] New Feature `.csproj` has correct MSBuild ItemGroups for platform-specific XAML splitting (see `platform-xaml.instructions.md`)

---

## Build Commands

```powershell
# Build entire solution
dotnet build MTM_Waitlist_Application.slnx

# Run Windows app
# Set startup project in VS 2026 → MTM_Waitlist_Application.WinUI → F5

# Run Android emulator
# Set startup project in VS 2026 → MTM_Waitlist_Application.Droid → F5

# Verify references on Shared project
dotnet list MTM_Waitlist_Application/MTM_Waitlist_Application/MTM_Waitlist_Application.csproj reference

# Confirm WMC1006 suppression is working
dotnet build MTM_Waitlist_Application.slnx 2>&1 | Select-String "WMC1006"
```

---

## Current Feature Implementation State

| Feature | Status | Files Present |
|---------|--------|---------------|
| Feature.Auth | 🚧 In Progress | ViewModel_Auth_Login, View_Auth_Login (Windows + Android + code-behind), app starts on login page |
| Feature.Dashboard | ✅ Complete | ViewModel_Dashboard_Main, View_Dashboard_Main (Windows + Android + code-behind) |
| Feature.Waitlist | 🚧 Empty stub | Platforms/ folder only — no Views, no ViewModels yet |
| Feature.Mobile | 🚧 Empty stub | Platforms/ folder only — no Views, no ViewModels yet |

## Current Test Project State

| Project | TFM | Status |
|---------|-----|--------|
| `Core.Tests` | `net10.0` | 🚧 Scaffold only — `UnitTest1.cs` placeholder |
| `Data.Tests` | `net10.0-windows10.0.19041.0` | ✅ Implemented repository + seeding tests |
| `Services.Tests` | `net10.0-windows10.0.19041.0` | ✅ Implemented auth, sync, and waitlist service tests |
| `Feature.Auth.Tests` | `net10.0-windows10.0.19041.0` | ✅ Implemented login ViewModel tests |
| `Feature.Dashboard.Tests` | `net10.0-windows10.0.19041.0` | ✅ Implemented ViewModel property tests |
| `Feature.Waitlist.Tests` | `net10.0-windows10.0.19041.0` | 🚧 Project exists but no source logic yet |
| `UITests.WinUI` | `net10.0-windows10.0.19041.0` | 🚧 Project exists but no authored UI tests yet |
| `UITests.Droid` | `net10.0` | 🚧 Project exists but no authored UI tests yet |

Test folder structure rule: mirror the source project path, then add a category subfolder at the leaf.
See `testing.instructions.md` for the full path mapping table and category definitions.

## Additional Resources

- Setup guide: `docs\ApplicationSetup\MAUI-SETUP.md`
- UI design differences: `docs\ApplicationSetup\UI-DESIGN-DIFFERENCES.md`
- Agent definitions: `AGENTS.md`
- Assumption files: `.github/assumptions/`
- Instruction files: `.github/instructions/`
  - `maui-architecture.instructions.md` — layer rules, naming, DI lifetimes
  - `platform-xaml.instructions.md` — exact .csproj MSBuild pattern for XAML splitting
  - `codebase-state.instructions.md` — what is actually built vs. stubbed
  - `database.instructions.md` — MySQL naming conventions, folder structure, procedure/trigger patterns
  - `testing.instructions.md` — xUnit / Moq / FluentAssertions patterns
- Prompt files: `.github/prompts/`
- Database files: `Database/` — MySQL schema, procedures, triggers, indexes, seed, migrations
- Server solution: `MTM_Waitlist_Server/` — implemented server admin app, REST API host, migrations, backup/restore, and database operations

## Server-Specific Instructions

- Maintain `MTM_Waitlist_Server` as a separate standalone solution inside this repository rather than including it in the main MAUI solution.
- The server-side DATABASE documents have been carried forward into the server solution and should be treated as implemented unless contradicted by source.
- For FEATURE-01 specifically: the client-side auth UI is now in `Feature.Auth`; workstation detection and silent auto-login remain dependent on server auth endpoint implementation.