# MTM Waitlist Application — AI Agent Definitions

Specialized agents for common development tasks. Reference these in Copilot Chat
by describing the task — Copilot will apply the relevant agent behavior.

---

## 🏗️ feature-scaffolder

**Purpose:** Scaffold a complete new feature module from a description.

**When to use:** You need to add a new screen or workflow (e.g., "I need a reports page").

**What it produces:**
- `MTM_Waitlist_Application.Feature.<Name>/` project (MAUI Class Library)
- `Views/<Screen>/View_<Feature>_<Screen>.Windows.xaml` + `.Android.xaml` + `.xaml.cs` (ONE shared code-behind)
- `ViewModels/<Screen>/ViewModel_<Feature>_<Screen>.cs`
- `MTM_Waitlist_Application.Core/Models/<Domain>/Model_<Entity>.cs`
- `MTM_Waitlist_Application.Core/Interfaces/<Domain>/IService_<Feature>.cs`
- `MTM_Waitlist_Application.Core/Interfaces/<Domain>/IRepository_<Entity>.cs` (online)
- `MTM_Waitlist_Application.Core/Interfaces/<Domain>/IRepository_<Entity>Local.cs` (offline)
- `MTM_Waitlist_Application.Services/<Domain>/Service_<Feature>.cs` (connectivity-aware, dual-repo)
- `MTM_Waitlist_Application.Data/Repositories/<Domain>/Repository_<Entity>.cs` (online via IApiClient)
- `MTM_Waitlist_Application.Data/Repositories/<Domain>/Repository_<Entity>Local.cs` (offline via LocalDbContext)
- Registration stubs in `AddSharedServices()`
- `.csproj` with CommunityToolkit.Mvvm 8.4.2, MVVMTK0045 suppression, platform XAML ItemGroups

**Folder rule enforced:**
All files go into a domain subfolder — never directly into the type root.
Pattern: `<TypeFolder>/<DomainSubfolder>/FileName.cs`

**Naming enforced:**
- `ViewModel_<Feature>_<Screen>` · `View_<Feature>_<Screen>`
- `Service_<Feature>` / `IService_<Feature>`
- `Repository_<Entity>` / `IRepository_<Entity>`
- `Model_<Entity>`

**Conversation starters:**
- "Scaffold a new Reports feature module"
- "Create the Waitlist Entry screen with ViewModel and service"
- "Add a new feature module for user profiles"

---

## 🔍 architecture-auditor

**Purpose:** Validate that the solution follows dependency rules and naming conventions.

**When to use:** After adding new code, before a PR, or when something feels wrong.

**What it checks:**
- Host projects only reference Shared
- Feature projects do not reference Data
- Feature projects do not reference other Features
- All ViewModels are `partial` and inherit `ObservableObject`
- All async methods end with `Async`
- No `new` instantiation of services/repos/ViewModels
- No runtime XAML bindings (missing `x:DataType`)
- All new registrations present in `AddSharedServices()`
- `WMC1006` suppressed in WinUI `.csproj`
- `MVVMTK0045` suppressed in Feature `.csproj` files that use CommunityToolkit.Mvvm
- `CommunityToolkit.Mvvm 8.4.2` present in Feature `.csproj` files with ViewModels
- Platform XAML ItemGroups present in Feature `.csproj` for each screen with split layouts
- Naming conventions match the `ViewModel_`, `Service_`, `Repository_`, `Model_` patterns
- All `.cs` and `.xaml` files are in a domain subfolder — no files placed directly in a type root
- Domain subfolder matches the file's feature or concern (e.g., `Waitlist`, `Auth`, `Sync`, `Api`, `Shared`)
- Service implementations use dual-repository pattern (online + local) with connectivity routing

**Conversation starters:**
- "Audit the solution for architecture violations"
- "Check that my new feature follows the dependency rules"
- "Validate naming conventions across all projects"

---

## 🧪 test-scaffolder

**Purpose:** Generate unit test stubs for a ViewModel, Service, Repository, or Model.

**When to use:** After implementing any testable class, before submitting.

**Test projects and their target frameworks:**

| Project | TFM | Tests |
|---|---|---|
| `Core.Tests` | `net10.0` | Models, enums, constants |
| `Data.Tests` | `net10.0-windows10.0.19041.0` | Repositories, HttpApiClient, LocalDbContext, MockDataSeeder |
| `Services.Tests` | `net10.0-windows10.0.19041.0` | Services (dual-repo + connectivity routing) |
| `Feature.Dashboard.Tests` | `net10.0-windows10.0.19041.0` | Dashboard ViewModels |
| `Feature.Waitlist.Tests` | `net10.0-windows10.0.19041.0` | Waitlist ViewModels |
| `UITests.WinUI` | `net10.0-windows10.0.19041.0` | WinUI app UI automation via Appium |
| `UITests.Droid` | `net10.0` | Android app UI automation via Appium |

**Folder structure enforced:**
Test file paths mirror the source project path, then add a **category subfolder** at the leaf:
```
Tests/Unit/<TestProject>/<SourceFolder>/<SourceSubfolder>/<Category>/<TestClassName>.cs
```

Valid category folders: `Success`, `Failure`, `Validation`, `Commands`, `Properties`, `Connectivity`, `AuthSeeds`, `WaitlistSeeds`

**Examples:**
```
// Source: Core/MTM_Waitlist_Application.Data/Mock/MockDataSeeder.cs
Tests/Unit/MTM_Waitlist_Application.Data.Tests/Mock/AuthSeeds/MockDataSeederTests.cs
Tests/Unit/MTM_Waitlist_Application.Data.Tests/Mock/WaitlistSeeds/MockDataSeederTests.cs

// Source: Core/MTM_Waitlist_Application.Services/Waitlist/Service_WaitlistEntry.cs
Tests/Unit/MTM_Waitlist_Application.Services.Tests/Waitlist/Success/Service_WaitlistEntryTests.cs
Tests/Unit/MTM_Waitlist_Application.Services.Tests/Waitlist/Failure/Service_WaitlistEntryTests.cs
Tests/Unit/MTM_Waitlist_Application.Services.Tests/Waitlist/Connectivity/Service_WaitlistEntryTests.cs

// Source: Features/.../ViewModels/Main/ViewModel_Dashboard_Main.cs
Tests/Unit/MTM_Waitlist_Application.Feature.Dashboard.Tests/ViewModels/Main/Commands/ViewModel_Dashboard_MainTests.cs
Tests/Unit/MTM_Waitlist_Application.Feature.Dashboard.Tests/ViewModels/Main/Properties/ViewModel_Dashboard_MainTests.cs
```

**What it produces:**
- One test file per source file per category — never mix categories in one file
- xUnit v3 test class with `[Fact]` and `[Theory]` patterns
- Mock setup for all injected interfaces using `Moq`
- Namespace matching the test file's folder path exactly
- Test data prefixed with `"TEST-"`

**Test naming enforced:**
`MethodName_Should<Result>_When<Condition>`

**Conversation starters:**
- "Scaffold tests for Service_WaitlistEntry"
- "Generate unit tests for ViewModel_Dashboard_Main commands"
- "Create failure-path tests for Repository_WaitlistEntryLocal"
- "Add connectivity tests for SyncService"

---

## 📝 doc-updater

**Purpose:** Update XML doc comments and inline comments after code changes.

**When to use:** After any code change to ensure documentation stays current.

**What it checks/updates:**
- `/// <summary>` on all public and internal classes
- `/// <summary>` + `/// <param>` + `/// <returns>` on all public methods
- Inline `// ──` section headers in `AddSharedServices()`
- Comments in `MauiProgram.cs` host files (platform-specific guidance)

**Conversation starters:**
- "Update doc comments for the new WaitlistEntry service"
- "Add XML docs to all public methods in Repository_WaitlistEntry"
- "Review comments in MauiProgramExtensions after my changes"

---

## 🔧 di-registrar

**Purpose:** Register a newly created class in `AddSharedServices()`.

**When to use:** After creating a new page, ViewModel, service, or repository.

**Rules enforced:**
- Repositories → `AddSingleton<IRepository_X, Repository_X>()`
- Services → `AddSingleton<IService_X, Service_X>()`
- ViewModels → `AddTransient<ViewModel_X_Y>()`
- Pages → `AddTransient<View_X_Y>()`
- Placement: under the correct `// ── Section ──` comment block

**Conversation starters:**
- "Register Service_WaitlistEntry and its repository in DI"
- "Add ViewModel_Dashboard_Summary to AddSharedServices"
- "Wire up the new reporting feature in the DI container"

---

## 🎨 xaml-platform-splitter

**Purpose:** Split a single XAML page into Windows and Android variants.

**When to use:** A page works on one platform but needs a different layout for the other.

**What it produces:**
- `View_<Feature>_<Screen>.Windows.xaml` — desktop layout (multi-column Grid, Flyout locked)
- `View_<Feature>_<Screen>.Android.xaml` — mobile layout (single-column, Flyout drawer)
- Shared `View_<Feature>_<Screen>.xaml.cs` code-behind (ViewModel binding only — no platform logic)
- Updated `.csproj` with platform XAML ItemGroups (see `platform-xaml.instructions.md`)

**Rules enforced:**
- Both XAML files bind to the same ViewModel — zero duplication of logic
- `x:DataType` present on both files
- Android minimum tap targets: 48px
- `OnIdiom` used only for minor property tweaks, not layout sections
- Navigation uses `FlyoutBehavior={OnPlatform WinUI=Locked, Android=Flyout}` in AppShell

**Conversation starters:**
- "Split View_Waitlist_Entry into Windows and Android layouts"
- "Create a desktop version of the dashboard page"
- "Add a mobile-optimized layout for the reports screen"

---

## �️ db-schema-designer

**Purpose:** Create or modify MySQL database objects (tables, procedures, triggers, indexes)
that match the C# codebase naming conventions and schema rules.

**When to use:** Adding a new table or domain, writing a stored procedure, adding an
index, creating a migration script, or reviewing the database schema for consistency.

**What it produces:**
- `Database/schema/tables/<Domain>/<TableName>.sql` — CREATE TABLE with all constraints
- `Database/indexes/<Domain>/<TableName>_Indexes.sql` — indexes for FK and WHERE columns
- `Database/procedures/<Domain>/usp_<Domain>_<Action>.sql` — one file per procedure
- `Database/triggers/<Domain>/trg_<TableName>_<Timing><Event>.sql` — one file per trigger
- `Database/migrations/V00N__<Description>.sql` — standalone migration for new objects
- **Updated `Database/README.md`** — Folder Structure, File Reference, and Execution Order sections updated for every new file
- Updated `AGENTS.md` and `copilot-agents.json` entries when a new domain is added

**Naming enforced (must match C# codebase PascalCase):**
- Tables: PascalCase plural — `WaitlistEntries`, `Users`, `RefreshTokens`
- Columns: PascalCase — `Id`, `FirstName`, `CreatedAt`, `IsActive`
- Primary key: `pk_<Table>`
- Unique: `uq_<Table>_<Column>`
- Foreign key: `fk_<Table>_<Reference>`
- Index: `idx_<Table>_<Column(s)>`
- Procedure: `usp_<Domain>_<Action>`
- Trigger: `trg_<Table>_<Timing><Event>`

**Rules enforced:**
- All datetimes in UTC via `UTC_TIMESTAMP()` — never `NOW()` or `TIMESTAMP` columns
- All tables include `Id`, `CreatedAt`, `UpdatedAt` (set by triggers)
- Passwords are never compared in SQL — hash returned to API for bcrypt verification
- Every FK column has an explicit index
- Every write procedure has `DECLARE EXIT HANDLER FOR SQLEXCEPTION ... RESIGNAL`
- Every file starts with `DROP ... IF EXISTS` so it is re-runnable

**Conversation starters:**
- "Add a Departments lookup table to the database"
- "Write a stored procedure to get entries by department"
- "Create a migration for adding a Notes column to WaitlistEntries"
- "Add an index on WaitlistEntries.Department"

---

## �📋 assumption-documenter

**Purpose:** Create a structured assumption file before proceeding with ambiguous work.

**When to use:** Any time a major assumption must be made before implementation.

**File location:** `.github/assumptions/`
**File naming:** `MMDDYYYY-HHMMam/pm-Assumptions.md`

**Required sections:**
1. Numbered list of assumptions
2. Why each assumption is needed
3. Impact if wrong
4. Alternative interpretations considered
5. Explicit confirmation request

**Conversation starters:**
- "Document my assumptions before scaffolding the reports module"
- "Create an assumption file for the database schema I'm about to design"
- "I'm not sure about the navigation flow — document assumptions first"