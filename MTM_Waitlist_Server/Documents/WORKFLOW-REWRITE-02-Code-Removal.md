# Glossary

- Backing logic: The code-behind, service, helper, and ViewModel behavior behind an existing UI surface.
- UI shell: Existing XAML layout and visible controls that must remain in place.
- Rewrite target: Code that should be removed or replaced during the workflow reset.

# Code To Remove Or Replace

## Rule

Do not remove the existing UI elements from the first-run page or migration page.
Remove or replace the current workflow logic behind them.

## First-Run Workflow

### Replace Completely

- `MTM_Waitlist_Server/Hosts/MTM_Waitlist_Server.Admin/Services/Service_FirstRun.cs`
  - Replace current probe logic, step-1 DB/user setup logic, and step-3 admin creation flow.
  - Remove the current mixed responsibilities around probing, user creation, and password reuse heuristics.

- `MTM_Waitlist_Server/Hosts/MTM_Waitlist_Server.Admin/ViewModels/ViewModel_FirstRun.cs`
  - Replace current step-state logic, app-user existence probing, and command orchestration.
  - Remove the current conditional field-enable behavior and status-message branching.

- `MTM_Waitlist_Server/Hosts/MTM_Waitlist_Server.Admin/Helpers/SqlScriptRunner.cs`
  - Replace current bootstrap execution behavior for first-run schema setup.
  - Remove the current ad hoc bootstrap tolerance behavior that grew around repeated failures.

### Keep As UI Shell / Lightweight Wiring

- `MTM_Waitlist_Server/Hosts/MTM_Waitlist_Server.Admin/Views/View_FirstRun.xaml`
  - Keep UI elements.
  - Rebind only if needed.

- `MTM_Waitlist_Server/Hosts/MTM_Waitlist_Server.Admin/Views/View_FirstRun.xaml.cs`
  - Keep only minimal step display and event wiring.

### Replace Decision Routing Around First-Run

- `MTM_Waitlist_Server/Hosts/MTM_Waitlist_Server.Admin/Services/Service_StartupCoordinator.cs`
  - Replace startup routing rules that depend on the old first-run assumptions.

- `MTM_Waitlist_Server/Hosts/MTM_Waitlist_Server.Admin/MainWindow.xaml.cs`
  - Review and replace only the first-run navigation/wizard-entry logic if it assumes old state transitions.

## Migration Workflow

### Replace Completely

- `MTM_Waitlist_Server/Hosts/MTM_Waitlist_Server.Admin/Services/Service_Migration.cs`
  - Remove the current version-first / rerun-idempotent-objects design.
  - Replace with a compare-live-vs-file workflow that surfaces missing and drifted objects.

- `MTM_Waitlist_Server/Core/MTM_Waitlist_Server.Core/Interfaces/Migration/IService_Migration.cs`
  - Replace the current contract with one oriented around scan/compare/update operations.

- `MTM_Waitlist_Server/Modules/MTM_Waitlist_Server.Module.Migrations/ViewModels/ViewModel_Migrations.cs`
  - Remove current pending-migration/applied-migration-only orchestration.
  - Replace with compare results, selected updates, and simple update execution state.

### Keep As UI Shell / Replace Backing Behavior

- `MTM_Waitlist_Server/Modules/MTM_Waitlist_Server.Module.Migrations/Views/View_Migrations.xaml`
  - Keep the page and existing visible sections.
  - Repurpose the migration content to reflect missing/drifted object comparison.

- `MTM_Waitlist_Server/Modules/MTM_Waitlist_Server.Module.Migrations/Views/View_Migrations.xaml.cs`
  - Keep only minimal page-load, preview, and confirmation-dialog wiring.

## Danger Zone Workflow

### Replace Completely

- The wipe/reset behavior inside `Service_Migration.cs`
  - Replace current reset implementation so it is part of the new compare/update model and first-run reset contract.

- The wipe command behavior inside `ViewModel_Migrations.cs`
  - Replace current status handling, post-reset reload behavior, and stale-state assumptions.

- The wipe confirmation handling inside `View_Migrations.xaml.cs`
  - Keep the dialog shell, but replace the backing execution flow if needed.

## SQL Rewrite Targets

### Rewrite As Source-Of-Truth Inputs

- `MTM_Waitlist_Server/Database/migrations/V001__Initial_Schema.sql`
  - Replace current bootstrap behavior with a clean baseline strategy that is safe to resume.

- `MTM_Waitlist_Server/Database/migrations/V002__Add_SchemaVersions_Table.sql`
  - Review whether it remains necessary under the simplified compare/update model.

- `MTM_Waitlist_Server/Database/migrations/V003__SetupTech_Schema.sql`
- `MTM_Waitlist_Server/Database/migrations/V004__SetupTech_Default_DunnageTypeConfig.sql`
  - Review whether these should stay as true migrations or be reduced in favor of source-of-truth comparison files.

## Tests To Replace Or Expand

- `MTM_Waitlist_Server/Tests/MTM_Waitlist_Server.Module.Migrations.Tests/UnitTest1.cs`
  - Replace the current structural and patch-regression assertions with tests for:
    - compare results,
    - drift detection,
    - safe update selection,
    - danger-zone reset routing,
    - first-run progression.

## Files Likely To Stay Mostly Intact

- `MTM_Waitlist_Server/Hosts/MTM_Waitlist_Server.Admin/App.xaml.cs`
  - Keep DI registration and host startup shell; only update registrations if contracts change.

- `MTM_Waitlist_Server/Hosts/MTM_Waitlist_Server.Admin/Services/Service_SettingsStore.cs`
  - Keep settings persistence unless the rewrite requires new fields.

- `MTM_Waitlist_Server/Core/MTM_Waitlist_Server.Core/Models/FirstRun/*`
  - Keep shared status/result models unless the new workflow needs different state names.

- `MTM_Waitlist_Server/Core/MTM_Waitlist_Server.Core/Models/Settings/*`
  - Keep settings models unless the new comparison workflow needs extra metadata.