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
- `Models/Model_<Entity>.cs`
- `Interfaces/IService_<Feature>.cs`
- `Interfaces/IRepository_<Entity>.cs`

### Services project
- `Service_<Feature>.cs`

### Data project
- `Repositories/Repository_<Entity>.cs`

### Feature project (`Feature.<Name>`)
- `Views/View_<Feature>_<Screen>.Windows.xaml` + `.Windows.xaml.cs`
- `Views/View_<Feature>_<Screen>.Android.xaml` + `.Android.xaml.cs`
- `Views/View_<Feature>_<Screen>.xaml.cs` (shared code-behind)
- `ViewModels/ViewModel_<Feature>_<Screen>.cs`

### Shared project
- Add registrations to `AddSharedServices()` in `MauiProgramExtensions.cs`

## Naming Convention Checklist
- [ ] ViewModel: `ViewModel_<Feature>_<Screen>`
- [ ] View: `View_<Feature>_<Screen>`
- [ ] Service: `Service_<Feature>` / `IService_<Feature>`
- [ ] Repository: `Repository_<Entity>` / `IRepository_<Entity>`
- [ ] Model: `Model_<Entity>`
- [ ] All async methods end with `Async`
- [ ] All classes have `/// <summary>` XML doc comments

## Architecture Checklist
- [ ] ViewModel inherits `ObservableObject` and is `partial`
- [ ] ViewModel uses `[ObservableProperty]` and `[RelayCommand]`
- [ ] Service interface in Core, implementation in Services
- [ ] Repository returns `Model_Dao_Result` — never throws
- [ ] Feature project references Services + Core only
- [ ] Registrations added to `AddSharedServices()` with correct lifetimes