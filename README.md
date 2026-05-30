# MTM Waitlist Application

MTM Waitlist Application is a .NET MAUI 10 solution for managing waitlist workflows across a Windows-primary experience with Android companion support.

The server-side admin application and REST API now live in the sibling solution folder `MTM_Waitlist_Server/` inside this repository. It is maintained as a separate solution and is not loaded into the MAUI `.slnx`.

---

## Overview

The repository has moved beyond the initial scaffold and now contains the modular foundation for the application:

- thin WinUI and Android host projects
- a shared MAUI startup and dependency injection hub
- Core, Data, Services, and Feature projects
- a dedicated `Feature.Auth` project for authentication UI
- a Dashboard feature with separate Windows and Android XAML layouts
- API client plumbing with primary and fallback base URL support
- authentication service wiring with secure token, refresh-token, and stored-session handling
- offline SQLite cache support for waitlist data
- connectivity-aware waitlist service routing
- offline write queue and sync service foundations
- debug mock-data seeding for UI development when the primary API is unavailable
- architecture, setup, UI, and Copilot guidance documentation
- a separate implemented server/admin solution under `MTM_Waitlist_Server/`

---

## Current Status

### Completed

- .NET MAUI multi-project application targeting .NET 10
- Windows host project targeting `net10.0-windows10.0.19041.0`
- Android host project targeting `net10.0-android`
- shared startup flow through `UseSharedMauiApp()`
- shared DI registration in `AddSharedServices()`
- embedded `appsettings.json` and `appsettings.Development.json` configuration loading
- Core project with waitlist, auth, API, sync, and result contracts
- Data project with HTTP API access, SQLite local storage, repositories, and debug mock data
- Services project with auth, waitlist, and sync services
- Feature.Auth project implemented for workstation-aware login UX
- Dashboard feature project with MVVM ViewModel and platform-specific XAML
- Waitlist and Mobile feature projects created for upcoming screens
- MTM Waitlist Server solution implemented separately in `MTM_Waitlist_Server/`
- database administration, migrations, backup/restore, kill switch, and server-hosted API implemented in the server solution
- WinUI `WMC1006` warning suppression in the WinUI project file

### In Progress / Next Phase

- expand `Model_WaitlistEntry` once the backend API contract is finalized
- expand role-specific post-login destinations as feature pages are added
- implement waitlist entry pages and ViewModels
- connect Dashboard summary cards to real service data
- add navigation routes and Shell entries for feature pages
- continue expanding unit and UI test coverage

---

## Solution Structure

```text
MTM_Waitlist_Application.slnx
└── MTM_Waitlist_Application/
    ├── MTM_Waitlist_Application/                         # Shared MAUI app, resources, Shell, DI
    ├── MTM_Waitlist_Application.Droid/                   # Android host launcher
    ├── MTM_Waitlist_Application.WinUI/                   # Windows host launcher
    ├── Core/
    │   ├── Core/                # Models, interfaces, constants
    │   ├── Data/                # API, SQLite, repositories, mock data
    │   └── Services/            # Business logic services
    └── Features/
        ├── Feature.Dashboard/   # Dashboard page and ViewModel
        ├── Feature.Auth/        # Authentication/login feature
        ├── Feature.Waitlist/    # Waitlist feature placeholder project
        └── Feature.Mobile/      # Mobile feature placeholder project
```

      Separate server solution:

      ```text
      MTM_Waitlist_Server/
      ├── Hosts/      # WinUI server admin app
      ├── Core/       # API + shared server contracts
      ├── Modules/    # Dashboard, Settings, Backup, KillSwitch, Migrations
      ├── Database/   # Executed server-side schema/procedures/triggers/indexes
      └── Tests/      # Server-side test projects
      ```

---

## Architecture

The application follows the documented MVVM and layered dependency flow:

```text
View / XAML → ViewModel → Service → Repository → API or SQLite
```

### Project Responsibilities

- **Host projects** (`.WinUI`, `.Droid`) contain platform launch code only and reference the shared MAUI project.
- **Shared project** configures MAUI, fonts, logging, embedded configuration, and dependency injection.
- **Core project** contains models, service interfaces, repository interfaces, result envelopes, and constants.
- **Data project** implements API communication, local SQLite storage, repositories, and debug mock data seeding.
- **Services project** owns business logic, connectivity decisions, authentication, and offline sync orchestration.
- **Feature projects** contain pages and ViewModels for app screens.

### Current Data Flow

- `HttpApiClient` selects the primary API URL first and falls back to the configured fallback URL when needed.
- Android emulator fallback URLs rewrite `localhost` to `10.0.2.2`.
- JWT bearer tokens are read from MAUI `SecureStorage` and attached to outbound API requests.
- valid stored sessions skip the login screen on startup.
- Windows startup now checks shared-workstation status and attempts silent auto-login for personal workstations.
- `Service_WaitlistEntry` uses the online repository when internet access is available.
- Local SQLite is used when offline or when online requests fail.
- Offline writes are queued locally for later replay by `SyncService` when connectivity returns.

---

## Startup and Dependency Injection

The application uses a shared startup pattern:

- `MTM_Waitlist_Application.WinUI/MauiProgram.cs`
- `MTM_Waitlist_Application.Droid/MauiProgram.cs`

Both host projects delegate shared setup to:

- `MTM_Waitlist_Application/MauiProgramExtensions.cs`

Shared setup currently handles:

- `UseMauiApp<App>()`
- font registration
- debug logging
- embedded JSON configuration loading
- `IHttpClientFactory` registration
- infrastructure, repository, service, page, and ViewModel registrations
- eager `ISyncService` resolution so connectivity events are subscribed at startup
- debug-only local mock data seeding when the primary API is unreachable
- startup selection between the login screen and `AppShell` based on stored auth session state

Rule of thumb:

- **Platform-specific setup** belongs in each host project's `MauiProgram.cs`.
- **Shared setup and all service registrations** belong in `MauiProgramExtensions.cs`.

---

## Configuration

API base URLs are configured in:

- `MTM_Waitlist_Application/MTM_Waitlist_Application/appsettings.json`
- `MTM_Waitlist_Application/MTM_Waitlist_Application/appsettings.Development.json`

Current keys:

```json
{
    "Api": {
        "PrimaryBaseUrl": "http://172.16.1.104:5000",
        "FallbackBaseUrl": "http://localhost:5000"
    }
}
```

These files are embedded resources so MAUI can load them consistently across Windows and Android.

---

## Build and Run

The repository now supports the same solution-first workflow in both Visual Studio 2026 and VS Code.

### Shared prerequisites

- .NET 10 SDK available on the machine and resolved through `global.json`
- .NET MAUI workload installed for command-line and VS Code builds
- Android SDK/emulator installed if you want to run the Android host
- In VS Code, install the recommended extensions from `.vscode/extensions.json`

The repo keeps `MTM_Waitlist_Application.slnx` as the single solution source of truth. The .NET 10 CLI supports `.slnx`, so Visual Studio 2026 and VS Code can build the same solution file.

### Build the solution from the CLI

```powershell
dotnet restore MTM_Waitlist_Application.slnx
dotnet build MTM_Waitlist_Application.slnx
dotnet test MTM_Waitlist_Application.slnx --no-build
```

### Visual Studio 2026

Open `MTM_Waitlist_Application.slnx`, then:

- set startup project to `MTM_Waitlist_Application.WinUI`
- choose `Windows Machine`
- press `F5`

### Visual Studio Code

Open the repository root in VS Code. Install `C# Dev Kit` and `.NET MAUI` when prompted. The repo includes `.vscode/tasks.json`, so `Terminal > Run Task` can restore, build, test, or build a specific host project without needing Visual Studio.

The repo also includes checked-in launch profiles in `.vscode/launch.json` for the WinUI and Android host projects. In VS Code, choose `C#: MTM Waitlist WinUI` or `C#: MTM Waitlist Android`, then press `F5`.

### Run on Android

Use either editor after the MAUI workload and Android SDK are installed:

- set startup project to `MTM_Waitlist_Application.Droid`
- choose an Android emulator
- press `F5`

### Verify or install MAUI workloads

```powershell
dotnet workload list
dotnet workload install maui
```

---

## Feature Status

### Dashboard

The Dashboard feature includes:

- `ViewModel_Dashboard_Main`
- shared code-behind using constructor-injected ViewModel binding
- Windows-specific layout: `View_Dashboard_Main.Windows.xaml`
- Android-specific layout: `View_Dashboard_Main.Android.xaml`
- compiled bindings with `x:DataType`
- placeholder summary cards and status messaging until live data is connected

### Waitlist

The Waitlist feature project exists and references Core and Services. Pages and ViewModels are the next implementation step.

### Auth

The Auth feature project now provides the login page, shared/manual workstation detection flow, Windows auto-login for personal workstations, stored-session startup bypass, and refresh-token persistence. The client still requires the server admin app / API host to be running for live sign-in, whether that is the LAN server at `172.16.1.104:5000` or a local development host on `localhost:5000`.

### Mobile

The Mobile feature project exists for Android-focused screens. Pages and ViewModels are the next implementation step.

---

## Documentation

The following documents support current and future development:

- [Documents/ApplicationSetup/MAUI-SETUP.md](./Documents/ApplicationSetup/MAUI-SETUP.md)  
  Setup guide, architecture rules, dependency structure, startup configuration, and project organization.

- [Documents/ApplicationSetup/UI-DESIGN-DIFFERENCES.md](./Documents/ApplicationSetup/UI-DESIGN-DIFFERENCES.md)  
  Windows and Android UI design guidance.

- [Documents/AndroidStartupWorkflowEdgeCases.md](./Documents/AndroidStartupWorkflowEdgeCases.md)  
  Android startup workflow and edge-case notes.

- [AGENTS.md](./AGENTS.md)  
  AI agent roles used to scaffold features, audit architecture, register DI services, update docs, and manage assumptions.

- [MTM_Waitlist_Server/README.md](./MTM_Waitlist_Server/README.md)  
  Server admin application, REST API, migration system, backup/restore, and database operations.

- [Documents/UserGuide/FEATURE-01-Authentication-Login-Guide.md](./Documents/UserGuide/FEATURE-01-Authentication-Login-Guide.md)  
  Draft user-facing login guide for shared and personal workstation sign-in.

If present in the repository, the following `.github` files also support future development:

- [`.github/copilot-instructions.md`](./.github/copilot-instructions.md)  
  Main GitHub Copilot guidance for architecture, naming conventions, MVVM rules, and dependency rules.

- [`.github/copilot-agents.json`](./.github/copilot-agents.json)  
  Copilot agent registry for specialized development tasks.

- [`.github/instructions/winui3-architecture.instructions.md`](./.github/instructions/winui3-architecture.instructions.md)  
  Focused MAUI architecture rules and layer responsibilities.

- [`.github/instructions/testing.instructions.md`](./.github/instructions/testing.instructions.md)  
  Testing conventions and patterns for ViewModels and Services.

- [`.github/prompts/new-feature.prompt.md`](./.github/prompts/new-feature.prompt.md)  
  Guided prompt for scaffolding a new feature module.

- [`.github/prompts/code-review.prompt.md`](./.github/prompts/code-review.prompt.md)  
  Guided prompt for reviewing changes against the project standards.

- [`.github/prompts/commit-message.prompt.md`](./.github/prompts/commit-message.prompt.md)  
  Guided prompt for creating consistent commit messages.

---

## Development Notes

- The solution uses the Visual Studio 2026 `.slnx` format.
- Host projects should remain thin launchers only.
- Shared registrations belong in `MauiProgramExtensions.cs`.
- New pages and ViewModels should be registered as transient dependencies.
- Repositories and services should be registered in the shared DI container.
- Feature ViewModels must go through service interfaces, not repositories.
- Feature projects must not reference the Data project directly.
- The WinUI warning `WMC1006` is suppressed in the WinUI project file.
- Debug builds can seed the local SQLite cache with mock waitlist entries when the primary API is unavailable.

---

## License

Add project licensing information here if applicable.
