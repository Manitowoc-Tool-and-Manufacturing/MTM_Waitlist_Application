---
applyTo: "**/*.{cs,xaml,csproj}"
---

# MAUI Architecture Instructions

## Layer Responsibilities

| Layer | Project | Responsibility |
|-------|---------|---------------|
| Host | `.WinUI` / `.Droid` | Platform-specific startup config only |
| Shared | `MTM_Waitlist_Application` | DI hub, navigation, global styles |
| Feature | `Feature.*` | XAML pages + ViewModels |
| Services | `.Services` | Business logic, orchestration |
| Data | `.Data` | Repository implementations, EF Core |
| Core | `.Core` | Models, interfaces, enums, constants |

## Naming Quick Reference

| Type | Pattern | Example |
|------|---------|---------|
| ViewModel | `ViewModel_<Feature>_<Screen>` | `ViewModel_Waitlist_Entry` |
| View | `View_<Feature>_<Screen>` | `View_Waitlist_Entry` |
| Service | `Service_<Purpose>` | `Service_WaitlistEntry` |
| Service interface | `IService_<Purpose>` | `IService_WaitlistEntry` |
| Repository | `Repository_<Entity>` | `Repository_WaitlistEntry` |
| Repository interface | `IRepository_<Entity>` | `IRepository_WaitlistEntry` |
| Model | `Model_<Entity>` | `Model_WaitlistEntry` |
| Enum | `Enum_<Category>` | `Enum_WaitlistStatus` |

## DI Lifetimes

| Type | Lifetime | Reason |
|------|----------|--------|
| Repository | `Singleton` | Stateless, reusable |
| Service | `Singleton` | Stateless business logic |
| ViewModel | `Transient` | New instance per navigation |
| Page (View) | `Transient` | New instance per navigation |

## Result Pattern

All repositories must return `Model_Dao_Result` or `Model_Dao_Result<T>`.
Never throw — always return a failure result.

```csharp
// ✅ Success
return Model_Dao_Result<List<Model_WaitlistEntry>>.Success(entries);

// ✅ Failure
return Model_Dao_Result<List<Model_WaitlistEntry>>.Failure(
    $"Failed to load entries: {ex.Message}");
```