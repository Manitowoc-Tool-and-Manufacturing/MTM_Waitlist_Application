# MTM Waitlist Application — AI Agent Definitions

Specialized agents for common development tasks. Reference these in Copilot Chat
by describing the task — Copilot will apply the relevant agent behavior.

---

## 🏗️ feature-scaffolder

**Purpose:** Scaffold a complete new feature module from a description.

**When to use:** You need to add a new screen or workflow (e.g., "I need a reports page").

**What it produces:**
- `MTM_Waitlist_Application.Feature.<Name>/` project (MAUI Class Library)
- `Views/<Screen>/View_<Feature>_<Screen>.Windows.xaml` + `.Android.xaml` + `.xaml.cs`
- `ViewModels/<Screen>/ViewModel_<Feature>_<Screen>.cs`
- `MTM_Waitlist_Application.Core/Models/<Domain>/Model_<Entity>.cs`
- `MTM_Waitlist_Application.Core/Interfaces/<Domain>/IService_<Feature>.cs`
- `MTM_Waitlist_Application.Core/Interfaces/<Domain>/IRepository_<Entity>.cs`
- `MTM_Waitlist_Application.Services/<Domain>/Service_<Feature>.cs`
- `MTM_Waitlist_Application.Data/Repositories/<Domain>/Repository_<Entity>.cs`
- Registration stubs in `AddSharedServices()`

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
- Naming conventions match the `ViewModel_`, `Service_`, `Repository_`, `Model_` patterns
- All `.cs` and `.xaml` files are in a domain subfolder — no files placed directly in a type root (e.g., `Models/Model_X.cs` without a subfolder is a violation)
- Domain subfolder matches the file's feature or concern (e.g., `Waitlist`, `Auth`, `Sync`, `Api`, `Shared`)

**Conversation starters:**
- "Audit the solution for architecture violations"
- "Check that my new feature follows the dependency rules"
- "Validate naming conventions across all projects"

---

## 🧪 test-scaffolder

**Purpose:** Generate unit test stubs for a ViewModel or Service.

**When to use:** After implementing a ViewModel or Service, before submitting.

**What it produces:**
- `ViewModel_<Feature>_<Screen>Tests.cs` — tests for commands and observable properties
- `Service_<Feature>Tests.cs` — tests for business logic methods
- xUnit test class structure with `[Fact]` and `[Theory]` patterns
- Mock setup for injected interfaces using `Moq`

**Test naming enforced:**
`MethodName_Should<Result>_When<Condition>`

**Conversation starters:**
- "Generate unit tests for ViewModel_Waitlist_Entry"
- "Scaffold test stubs for Service_WaitlistEntry"
- "Create tests for the approve entry command"

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
- `View_<Feature>_<Screen>.Windows.xaml` — desktop layout (multi-column, flyout nav)
- `View_<Feature>_<Screen>.Android.xaml` — mobile layout (single-column, tab nav)
- Shared `View_<Feature>_<Screen>.xaml.cs` code-behind (ViewModel binding only)

**Rules enforced:**
- Both XAML files bind to the same ViewModel — zero duplication of logic
- `x:DataType` present on both files
- Android minimum tap targets: 48px
- `OnIdiom` used only for minor property tweaks, not layout sections

**Conversation starters:**
- "Split View_Waitlist_Entry into Windows and Android layouts"
- "Create a desktop version of the dashboard page"
- "Add a mobile-optimized layout for the reports screen"

---

## 📋 assumption-documenter

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