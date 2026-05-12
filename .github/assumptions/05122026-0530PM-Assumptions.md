# Assumptions — WinUI Host Conversion (MAUI to Standalone WinUI 3)

**Date:** 05/12/2026 5:30 PM
**Updated:** 05/12/2026 — full documentation audit completed
**Request:** Convert the Windows host to full standalone WinUI 3 (like the MTM_WinUi_Template pattern). Keep Android as MAUI. Full WinUI 3 UI is desired; lift confirmed moderate.

---

## Codebase Research Findings

| Item | Finding |
|------|---------|
| WinUI host today | `UseMaui=true` + `MauiWinUIApplication` — MAUI-bootstrapped, not standalone |
| MTM_WinUi_Template | Standalone WinUI 3: `UseWinUI=true`, no `UseMaui`, `Microsoft.UI.Xaml.Application`, `Window` + `Frame` |
| Feature.Auth ViewModel | Only depends on `CommunityToolkit.Mvvm` + `Core` — fully portable to WinUI 3 |
| Feature.Dashboard ViewModel | Only depends on `CommunityToolkit.Mvvm` + `Core` — fully portable to WinUI 3 |
| Feature.Auth Windows XAML | MAUI `ContentPage` with MAUI controls — must be rewritten as WinUI 3 `Page` |
| Feature.Dashboard Windows XAML | MAUI `ContentPage` with MAUI controls — must be rewritten as WinUI 3 `Page` |
| Feature.Waitlist | Empty stub — nothing to rewrite |
| Shared project | `MauiProgramExtensions`, `AppShell`, `App` are all MAUI constructs — cannot be used by a WinUI 3 host |

---

## Lift Assessment

**Verdict: Moderate — not a huge lift.**

ViewModels are already portable. Only 2 screens are implemented (Login + Dashboard). Android XAML files are completely untouched. Estimated: ~10–14 source file changes + 8–10 documentation file updates. No new projects required.

---

## Confirmed Assumptions

### 1. WinUI host becomes fully standalone WinUI 3
Removes `UseMaui=true`, `MauiWinUIApplication`, and `Microsoft.Maui.Controls`. Uses `Microsoft.WindowsAppSDK`, `Microsoft.UI.Xaml.Application`, and `Window` + `Frame` navigation — matching the `MTM_WinUi_Template` pattern.

### 2. Shared project is no longer referenced by the WinUI host
`MauiProgramExtensions.cs`, `AppShell.xaml`, and `App.xaml` in the Shared project are all MAUI constructs. The WinUI host builds its own composition root in `App.xaml.cs` using a `ServiceCollection` directly.

### 3. WinUI 3 views live inside the WinUI host project
WinUI 3 `Page` views for each screen live in `MTM_Waitlist_Application.WinUI\Views\`. The WinUI host references Feature projects for ViewModels only. The Windows MAUI XAML files (`*.Windows.xaml`) in Feature projects are deleted.

**Architecture rule updated:** The WinUI host may reference Feature projects solely to consume their ViewModels. It must never instantiate MAUI types.

### 4. Feature projects drop the `net10.0-windows10.0.19041.0` target
`TargetFrameworks` trimmed to Android only (matching the Droid host). Any Windows-conditional code in Feature projects reviewed and removed or moved to the WinUI host.

### 5. DI uses `Microsoft.Extensions.DependencyInjection` in `App.xaml.cs`
The WinUI host builds a `ServiceCollection`, registers `Core`, `Data`, `Services`, and ViewModels, then provides a static `Services` property on `App` — same pattern as the MTM v2.0 app and `MTM_WinUi_Template`.

### 6. NavigationView shell replaces MAUI Shell/Flyout
`MainWindow.xaml` uses a `NavigationView` (left sidebar, locked) and a `Frame` for page navigation — matching the Windows-primary design intent in the copilot instructions.

### 7. All documentation files updated
Every affected instruction file, agent definition, prompt, and documentation guide updated to reflect the new WinUI host boundary and architecture.

---

## Complete Files Affected

### Source Code

| File | Action |
|------|--------|
| `MTM_Waitlist_Application.WinUI\MTM_Waitlist_Application.WinUI.csproj` | Rewrite — remove MAUI, add Windows App SDK |
| `MTM_Waitlist_Application.WinUI\App.xaml` | Rewrite — WinUI 3 `Application` |
| `MTM_Waitlist_Application.WinUI\App.xaml.cs` | Rewrite — DI container + `OnLaunched` |
| `MTM_Waitlist_Application.WinUI\MauiProgram.cs` | **Delete** — no longer needed |
| `MTM_Waitlist_Application.WinUI\MainWindow.xaml` *(new)* | Create — `NavigationView` shell |
| `MTM_Waitlist_Application.WinUI\MainWindow.xaml.cs` *(new)* | Create — `Frame` navigation logic |
| `MTM_Waitlist_Application.WinUI\Views\Login\LoginPage.xaml` *(new)* | Create — WinUI 3 login page |
| `MTM_Waitlist_Application.WinUI\Views\Login\LoginPage.xaml.cs` *(new)* | Create |
| `MTM_Waitlist_Application.WinUI\Views\Dashboard\DashboardPage.xaml` *(new)* | Create — WinUI 3 dashboard |
| `MTM_Waitlist_Application.WinUI\Views\Dashboard\DashboardPage.xaml.cs` *(new)* | Create |
| `Feature.Auth\Feature.Auth.csproj` | Remove Windows `TargetFramework` + Windows XAML `ItemGroup` entries |
| `Feature.Auth\Views\Login\View_Auth_Login.Windows.xaml` | **Delete** — replaced by `LoginPage.xaml` in WinUI host |
| `Feature.Dashboard\Feature.Dashboard.csproj` | Remove Windows `TargetFramework` + Windows XAML `ItemGroup` entries |
| `Feature.Dashboard\Views\Main\View_Dashboard_Main.Windows.xaml` | **Delete** — replaced by `DashboardPage.xaml` in WinUI host |

### GitHub Instructions + Copilot Rules

| File | Change Needed |
|------|---------------|
| `.github\copilot-instructions.md` | Update dependency table (WinUI host rule), host layer description, DI section, Current Feature State table, build commands, XAML guidelines section |
| `.github\instructions\codebase-state.instructions.md` | Update DI registrations (WinUI now has its own DI), Startup Flow section, Feature.Auth + Feature.Dashboard view file list (remove `*.Windows.xaml` entries, note they are in the WinUI host) |
| `.github\instructions\maui-architecture.instructions.md` | Update layer table (WinUI host is no longer thin MAUI launcher; now owns views and DI root), note WinUI host references Feature projects for ViewModels only |
| `.github\instructions\platform-xaml.instructions.md` | Add explicit note that Windows XAML splitting is no longer used in Feature projects; Windows target removed; Windows views live in WinUI host |
| `.github\instructions\testing.instructions.md` | Note that `UITests.WinUI` now targets the standalone WinUI 3 app; no MAUI dependency on that test project |

### Agent + Prompt Files

| File | Change Needed |
|------|---------------|
| `AGENTS.md` | Update `feature-scaffolder`: new features no longer produce `*.Windows.xaml` in Feature projects — Windows views are authored in the WinUI host. Update `architecture-auditor`: WinUI host no longer references Shared; WinUI host may reference Feature projects for ViewModels only |
| `.github\prompts\new-feature.prompt.md` | Update scaffold instructions: Windows XAML now in WinUI host `Views\` folder, not Feature project; Feature project is Android-only target; remove Windows XAML `ItemGroup` pattern from template |
| `.github\prompts\code-review.prompt.md` | Update host project dependency rule: WinUI host references Feature projects for ViewModels (not Shared); add check that WinUI host never instantiates MAUI types |

### Documentation + Setup Guides

| File | Change Needed |
|------|---------------|
| `README.md` | Update solution overview (WinUI is standalone WinUI 3, not thin MAUI launcher); update Completed / In Progress sections to reflect conversion |
| `Documents\ApplicationSetup\MAUI-SETUP.md` | Update solution structure diagram and WinUI host description; add note about standalone WinUI 3 host alongside MAUI Android host |
| `Documents\ApplicationSetup\UI-DESIGN-DIFFERENCES.md` | Update core principle section (Windows views now live in WinUI host `Views\`, not Feature projects); update folder structure example to reflect new layout |
| `Documents\features\FEATURE-01-Authentication-Login.md` | Add note that the Windows login view (`LoginPage.xaml`) now lives in the WinUI host, not `Feature.Auth`; update the "What to Build" Windows layout section accordingly |
| `Documents\features\FEATURE-INDEX.md` | No structural change — status field for Feature-01 may be updated if applicable |
| `Documents\UserGuide\FEATURE-01-Authentication-Login-Guide.md` | Review for platform-specific guidance; update if Windows-specific steps reference the old MAUI host behavior |

---

## Status: Ready for implementation — no further confirmation needed.

All assumptions align with user intent (full WinUI 3 like the server app, lift confirmed moderate).
