---
mode: ask
description: Review code changes for MTM Waitlist Application architecture compliance
---

# Code Review — MTM Waitlist Application

Review the provided code or diff against the MTM Waitlist Application standards.

## Architecture Checks

### Dependency Rules
- [ ] Host projects (.WinUI / .Droid) only reference Shared
- [ ] Feature projects do not reference Data
- [ ] Feature projects do not reference other Features
- [ ] No `new` instantiation of services, repositories, or ViewModels

### MVVM Compliance
- [ ] ViewModel is `partial` and inherits `ObservableObject`
- [ ] Uses `[ObservableProperty]` — no manual `INotifyPropertyChanged`
- [ ] Uses `[RelayCommand]` — no manual `ICommand`
- [ ] No business logic in XAML code-behind
- [ ] No ViewModel → Repository calls (must go via Service)

### Naming Conventions
- [ ] ViewModels: `ViewModel_<Feature>_<Screen>`
- [ ] Views: `View_<Feature>_<Screen>`
- [ ] Services: `Service_<Purpose>` / `IService_<Purpose>`
- [ ] Repositories: `Repository_<Entity>` / `IRepository_<Entity>`
- [ ] Models: `Model_<Entity>`
- [ ] Async methods end with `Async`

### XAML
- [ ] `x:DataType` present — no runtime bindings
- [ ] Explicit binding `Mode` specified
- [ ] Android tap targets ≥ 48px
- [ ] No business logic in code-behind

### DI Registration
- [ ] New services registered as `Singleton`
- [ ] New repositories registered as `Singleton`
- [ ] New ViewModels registered as `Transient`
- [ ] New pages registered as `Transient`
- [ ] All registrations in `AddSharedServices()` only

### Code Quality
- [ ] All braces present (no braceless `if`)
- [ ] Explicit accessibility modifiers
- [ ] XML doc comments on public and internal classes/methods
- [ ] Collection reloads replace instance (not `Clear()` + `Add()`)
- [ ] Null checks use `is null` / `is not null`

## Output Format
For each violation found, report:
1. File path and line number
2. Rule violated
3. Suggested fix with corrected code snippet