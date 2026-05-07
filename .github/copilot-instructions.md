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

- **Framework:** .NET MAUI
- **Language:** C# 13
- **Platform:** .NET 10
- **Architecture:** MVVM with CommunityToolkit.Mvvm
- **Solution format:** `.slnx` (VS 2026)
- **Targets:** Windows (WinUI) primary · Android companion
- **DI Container:** Microsoft.Extensions.DependencyInjection

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
│   ├── MTM_Waitlist_Application.Core      ← models, interfaces, constants — zero dependencies
│   ├── MTM_Waitlist_Application.Services  ← business logic — references Core only
│   └── MTM_Waitlist_Application.Data      ← repositories, EF Core — references Core only
└── Features/
    ├── MTM_Waitlist_Application.Feature.Waitlist    ← XAML + ViewModels
    ├── MTM_Waitlist_Application.Feature.Dashboard   ← XAML + ViewModels
    └── MTM_Waitlist_Application.Feature.Mobile      ← Android-only screens
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

| What | Where |
|------|-------|
| Model / entity | `MTM_Waitlist_Application.Core/Models/` |
| Repository interface | `MTM_Waitlist_Application.Core/Interfaces/` |
| Service interface | `MTM_Waitlist_Application.Core/Interfaces/` |
| Constants | `MTM_Waitlist_Application.Core/Constants/` |
| Enums | `MTM_Waitlist_Application.Core/Enums/` |
| Repository implementation | `MTM_Waitlist_Application.Data/Repositories/` |
| EF Core DbContext | `MTM_Waitlist_Application.Data/` |
| Service implementation | `MTM_Waitlist_Application.Services/` |
| Feature XAML page | `MTM_Waitlist_Application.Feature.<Name>/Views/` |
| Feature ViewModel | `MTM_Waitlist_Application.Feature.<Name>/ViewModels/` |
| Mobile-only pages | `MTM_Waitlist_Application.Feature.Mobile/Views/` |
| Global styles / themes | `MTM_Waitlist_Application/Resources/Styles/` |
| Shared images / fonts | `MTM_Waitlist_Application/Resources/` |

---

## Architecture Patterns

### ViewModel Pattern

```csharp
// ✅ CORRECT
namespace MTM_Waitlist_Application.Feature.Waitlist.ViewModels;

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

```csharp
// ✅ CORRECT
namespace MTM_Waitlist_Application.Services;

/// <summary>
/// Business logic service for waitlist entry operations.
/// Abstracts repository access from ViewModels.
/// </summary>
public sealed class Service_WaitlistEntry : IService_WaitlistEntry
{
    private readonly IRepository_WaitlistEntry _repository;

    public Service_WaitlistEntry(IRepository_WaitlistEntry repository)
    {
        _repository = repository;
    }

    public async Task<Model_Dao_Result<List<Model_WaitlistEntry>>> GetAllEntriesAsync()
        => await _repository.GetAllWaitlistEntriesAsync();
}
```

### Repository Pattern

```csharp
// ✅ CORRECT — instance-based, returns result, never throws
namespace MTM_Waitlist_Application.Data.Repositories;

/// <summary>
/// Repository for waitlist entry data access.
/// All methods return Model_Dao_Result — never throw exceptions.
/// </summary>
public sealed class Repository_WaitlistEntry : IRepository_WaitlistEntry
{
    private readonly AppDbContext _context;

    public Repository_WaitlistEntry(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Model_Dao_Result<List<Model_WaitlistEntry>>> GetAllWaitlistEntriesAsync()
    {
        try
        {
            var entries = await _context.WaitlistEntries.ToListAsync();
            return Model_Dao_Result<List<Model_WaitlistEntry>>.Success(entries);
        }
        catch (Exception ex)
        {
            return Model_Dao_Result<List<Model_WaitlistEntry>>.Failure(
                $"Failed to retrieve waitlist entries: {ex.Message}");
        }
    }
}
```

### XAML Binding Pattern

```xml
<!-- ✅ CORRECT — compiled bindings with x:DataType -->
<ContentPage
    xmlns:vm="clr-namespace:MTM_Waitlist_Application.Feature.Waitlist.ViewModels"
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

```csharp
// ✅ CORRECT — all registrations here, nowhere else
public static IServiceCollection AddSharedServices(this IServiceCollection services)
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
- Sidebar `FlyoutItem` navigation in `AppShell.xaml`
- Data-dense `CollectionView` with multiple columns
- `MenuFlyout` for right-click context menus
- Side-by-side `Label` + input control forms
- Minimum button height: 36px

### Android Layout Rules
- Single-column `StackLayout` — one task per screen
- Bottom `TabBar` navigation in `AppShell.xaml`
- Card-based single-column `CollectionView`
- `SwipeView` for item actions
- Stacked `Label` above input control forms
- Minimum tap target: **48px height always**

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

## Additional Resources

- Setup guide: `MAUI-SETUP.md`
- UI design differences: `UI-DESIGN-DIFFERENCES.md`
- Agent definitions: `AGENTS.md`
- Assumption files: `.github/assumptions/`
- Instruction files: `.github/instructions/`
- Prompt files: `.github/prompts/`