# MTM Waitlist Application

MTM Waitlist Application is a .NET MAUI solution for managing waitlist workflows across a Windows-primary experience with Android companion support.

---

## Overview

This repository contains the foundational setup for the MTM Waitlist Application, including:

- a working .NET MAUI multi-project application
- Windows host support
- Android emulator support
- modular solution planning for future feature growth
- shared startup and dependency injection structure
- GitHub Copilot guidance, prompts, and agent setup
- project documentation for architecture, UI direction, and setup

At this stage, the application foundation is in place and ready for feature implementation.

---

## Current Status

### Completed
- .NET MAUI multi-project app scaffolded successfully
- Windows app builds and runs
- Android emulator app builds and runs
- shared startup flow established through `UseSharedMauiApp()`
- platform-specific startup split between WinUI and Droid hosts
- architecture and dependency rules documented
- setup guide aligned to actual Visual Studio 2026 scaffolded structure
- GitHub Copilot instructions, prompts, and agent definitions added
- initial documentation created for setup and platform UI planning

### Next Phase
The next phase is implementing actual application features, such as:

- waitlist entry screens
- dashboard screens
- shared models
- services
- repositories / data access
- navigation workflows
- business rules
- testing

---

## Solution Structure

```text
MTM_Waitlist_Application.slnx
└── MTM_Waitlist_Application/
    ├── MTM_Waitlist_Application/          # Shared project
    ├── MTM_Waitlist_Application.Droid/    # Android host
    └── MTM_Waitlist_Application.WinUI/    # Windows host
```

### Planned Modular Structure

```text
MTM_Waitlist_Application.slnx
├── Hosts/
│   ├── MTM_Waitlist_Application.WinUI
│   └── MTM_Waitlist_Application.Droid
├── Shared/
│   └── MTM_Waitlist_Application
├── Core/
│   ├── MTM_Waitlist_Application.Core
│   ├── MTM_Waitlist_Application.Services
│   └── MTM_Waitlist_Application.Data
└── Features/
    ├── MTM_Waitlist_Application.Feature.Waitlist
    ├── MTM_Waitlist_Application.Feature.Dashboard
    └── MTM_Waitlist_Application.Feature.Mobile
```

---

## How Startup Works

The application uses a shared startup pattern:

- `MTM_Waitlist_Application.WinUI/MauiProgram.cs`
- `MTM_Waitlist_Application.Droid/MauiProgram.cs`

Both host projects call the shared setup in:

- `MTM_Waitlist_Application/MauiProgramExtensions.cs`

### Rule of Thumb
- **Platform-specific setup** goes in each host project's `MauiProgram.cs`
- **Shared setup** goes in `UseSharedMauiApp()` and `AddSharedServices()`

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

## Documentation

The following documents are especially relevant to the work completed so far:

- [docs\ApplicationSetup\MAUI-SETUP.md](./docs\ApplicationSetup\MAUI-SETUP.md)  
  Step-by-step setup guide, architecture rules, dependency structure, startup configuration, and project organization.

- [docs\ApplicationSetup\UI-DESIGN-DIFFERENCES.md](./docs\ApplicationSetup\UI-DESIGN-DIFFERENCES.md)  
  Explains how Windows and Android UI design differ and how those differences should be handled in code.

- [AGENTS.md](./AGENTS.md)  
  Defines the AI agent roles used to help scaffold features, audit architecture, register DI services, update docs, and manage assumptions.

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

## Summary of Tonight’s Work

Tonight’s work focused on getting the application foundation built correctly before feature development begins.

### Accomplished
- set up and confirmed the MAUI application structure
- got Windows and Android runs working
- reviewed and corrected architecture guidance
- aligned documentation with the real scaffolded file layout
- clarified project reference rules and startup responsibilities
- added development guidance for GitHub Copilot and future AI-assisted work
- created setup and UI planning documentation for the next development phase

In simple terms: the application’s foundation is now in place, working, and documented clearly enough to begin building real features.

---

## Notes

- The solution currently uses the Visual Studio 2026 `.slnx` format
- Host projects should remain thin launchers only
- Shared registrations belong in `MauiProgramExtensions.cs`
- Future features should follow the documented naming and layering conventions
- The WinUI warning `WMC1006` may appear as a false-positive during Android-focused workflows and should be handled in the WinUI project file if needed

---

## License

Add project licensing information here if applicable.
