---
mode: agent
description: Scaffold a complete new MAUI feature module with correct MTM naming conventions
---

# New Feature Module Scaffold

Scaffold a complete feature module for the MTM Waitlist Application.

## Required Information
Before starting, confirm:
1. Feature name (e.g., `Reports`, `UserProfile`, `Notifications`)
2. Screen name (e.g., `List`, `Detail`, `Summary`)
3. Entity name (e.g., `Report`, `UserProfile`)
4. Is this Windows-only, Android-only, or both platforms?

## Files to Create

### Core project
- `Models/<Domain>/Model_<Entity>.cs`
- `Interfaces/<Domain>/IService_<Feature>.cs`
- `Interfaces/<Domain>/IRepository_<Entity>.cs`
- `Interfaces/<Domain>/IRepository_<Entity>Local.cs`

### Services project
- `<Domain>/Service_<Feature>.cs`

### Data project
- `Repositories/<Domain>/Repository_<Entity>.cs` (online via IApiClient)
- `Repositories/<Domain>/Repository_<Entity>Local.cs` (offline via LocalDbContext)

### Feature project (`Feature.<Name>`)
- `Views/<Screen>/View_<Feature>_<Screen>.Windows.xaml`
- `Views/<Screen>/View_<Feature>_<Screen>.Android.xaml`
- `Views/<Screen>/View_<Feature>_<Screen>.xaml.cs` (**one shared code-behind only** — no separate Windows/Android .xaml.cs files)
- `ViewModels/<Screen>/ViewModel_<Feature>_<Screen>.cs`
- Update `MTM_Waitlist_Application.Feature.<Name>.csproj` with CommunityToolkit.Mvvm, MVVMTK0045 suppression, and platform XAML ItemGroups

### Shared project
- Add registrations to `AddSharedServices()` in `MauiProgramExtensions.cs`
- Add `ShellContent` entry to `AppShell.xaml`

## Naming Convention Checklist
- [ ] ViewModel: `ViewModel_<Feature>_<Screen>` (in `ViewModels/<Screen>/`)
- [ ] View: `View_<Feature>_<Screen>` (in `Views/<Screen>/`)
- [ ] Service: `Service_<Feature>` / `IService_<Feature>`
- [ ] Repository online: `Repository_<Entity>` / `IRepository_<Entity>`
- [ ] Repository local: `Repository_<Entity>Local` / `IRepository_<Entity>Local`
- [ ] Model: `Model_<Entity>`
- [ ] All async methods end with `Async`
- [ ] All classes have `/// <summary>` XML doc comments

## Architecture Checklist
- [ ] ViewModel inherits `ObservableObject` and is `partial`
- [ ] ViewModel uses `[ObservableProperty]` and `[RelayCommand]`
- [ ] Service is connectivity-aware (routes online/local via `IConnectivity.NetworkAccess`)
- [ ] Online repository delegates to `IApiClient` only
- [ ] Local repository delegates to `LocalDbContext` (sqlite-net-pcl) only
- [ ] Both repositories return `Model_Dao_Result` — never throw
- [ ] Feature project references Services + Core only (NOT Data)
- [ ] Feature `.csproj` has `CommunityToolkit.Mvvm 8.4.2` package reference
- [ ] Feature `.csproj` has `MVVMTK0045` suppressed
- [ ] Feature `.csproj` has platform XAML ItemGroups (see `platform-xaml.instructions.md`)
- [ ] Registrations added to `AddSharedServices()` with correct lifetimes
- [ ] `ShellContent` entry added to `AppShell.xaml`