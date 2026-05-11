# MTM Waitlist Application

MTM Waitlist Application is a .NET MAUI 10 solution for managing waitlist workflows across a Windows-primary experience with Android companion support.

---

## Overview

The repository has moved beyond the initial scaffold and now contains the modular foundation for the application:

- thin WinUI and Android host projects
- a shared MAUI startup and dependency injection hub
- Core, Data, Services, and Feature projects
- a Dashboard feature with separate Windows and Android XAML layouts
- API client plumbing with primary and fallback base URL support
- authentication service wiring with secure token storage
- offline SQLite cache support for waitlist data
- connectivity-aware waitlist service routing
- offline write queue and sync service foundations
- debug mock-data seeding for UI development when the primary API is unavailable
- architecture, setup, UI, and Copilot guidance documentation

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
- Dashboard feature project with MVVM ViewModel and platform-specific XAML
- Waitlist and Mobile feature projects created for upcoming screens
- WinUI `WMC1006` warning suppression in the WinUI project file

### In Progress / Next Phase

- expand `Model_WaitlistEntry` once the backend API contract is finalized
- implement waitlist entry pages and ViewModels
- connect Dashboard summary cards to real service data
- add navigation routes and Shell entries for feature pages
- add unit tests for services, repositories, and ViewModels
- complete production API integration and validation rules

---

## Solution Structure

```text
MTM_Waitlist_Application.slnx
└── MTM_Waitlist_Application/
    ├── MTM_Waitlist_Application/                         # Shared MAUI app, resources, Shell, DI
    ├── MTM_Waitlist_Application.Droid/                   # Android host launcher
    ├── MTM_Waitlist_Application.WinUI/                   # Windows host launcher
    ├── Core/
    │   ├── MTM_Waitlist_Application.Core/                # Models, interfaces, constants
    │   ├── MTM_Waitlist_Application.Data/                # API, SQLite, repositories, mock data
    │   └── MTM_Waitlist_Application.Services/            # Business logic services
    └── Features/
        ├── MTM_Waitlist_Application.Feature.Dashboard/   # Dashboard page and ViewModel
        ├── MTM_Waitlist_Application.Feature.Waitlist/    # Waitlist feature placeholder project
        └── MTM_Waitlist_Application.Feature.Mobile/      # Mobile feature placeholder project
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

### Build the solution

```powershell
dotnet build MTM_Waitlist_Application.slnx
```

### Run on Windows

Open the solution in Visual Studio 2026 and:

- set startup project to `MTM_Waitlist_Application.WinUI`
- choose `Windows Machine`
- press `F5`

### Run on Android

Open the solution in Visual Studio 2026 and:

- set startup project to `MTM_Waitlist_Application.Droid`
- choose an Android emulator
- press `F5`

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

If present in the repository, the following `.github` files also support future development:

- [`.github/copilot-instructions.md`](./.github/copilot-instructions.md)  
  Main GitHub Copilot guidance for architecture, naming conventions, MVVM rules, and dependency rules.

- [`.github/copilot-agents.json`](./.github/copilot-agents.json)  
  Copilot agent registry for specialized development tasks.

- [`.github/instructions/maui-architecture.instructions.md`](./.github/instructions/maui-architecture.instructions.md)  
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
