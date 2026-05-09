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

---

## File Placement Rules

All `.cs` and `.xaml` files **must** live in a domain subfolder within their type folder.
Files placed directly in a type root are a violation.

**Pattern:** `<TypeFolder>/<DomainSubfolder>/FileName.cs`

### Core (`MTM_Waitlist_Application.Core/`)

| Type | Path pattern |
|------|--------------|
| Model | `Models/<Domain>/Model_<Entity>.cs` |
| Repository interface | `Interfaces/<Domain>/IRepository_<Entity>.cs` |
| Service interface | `Interfaces/<Domain>/IService_<Purpose>.cs` |
| Constants | `Constants/<Domain>/Constants_<Category>.cs` |
| Enums | `Enums/<Domain>/Enum_<Category>.cs` |

| File | Correct path |
|------|--------------|
| `Model_WaitlistEntry` | `Models/Waitlist/Model_WaitlistEntry.cs` |
| `Model_AuthToken` | `Models/Auth/Model_AuthToken.cs` |
| `Model_Dao_Result` | `Models/Shared/Model_Dao_Result.cs` |
| `IApiClient` | `Interfaces/Api/IApiClient.cs` |
| `IService_Auth` | `Interfaces/Auth/IService_Auth.cs` |
| `IRepository_WaitlistEntry` | `Interfaces/Waitlist/IRepository_WaitlistEntry.cs` |
| `IService_WaitlistEntry` | `Interfaces/Waitlist/IService_WaitlistEntry.cs` |
| `ISyncService` | `Interfaces/Sync/ISyncService.cs` |
| `Constants_Api` | `Constants/Api/Constants_Api.cs` |

### Data (`MTM_Waitlist_Application.Data/`)

| Type | Path |
|------|------|
| HTTP API client | `Http/HttpApiClient.cs` |
| Local DB context | `Local/LocalDbContext.cs` |
| Repository | `Repositories/<Domain>/Repository_<Entity>.cs` |

### Services (`MTM_Waitlist_Application.Services/`)

| Type | Path pattern |
|------|--------------|
| Service | `<Domain>/Service_<Purpose>.cs` |

Examples: `Auth/Service_Auth.cs` · `Waitlist/Service_WaitlistEntry.cs` · `Sync/SyncService.cs`

### Features (`MTM_Waitlist_Application.Feature.<Name>/`)

| Type | Path pattern |
|------|--------------|
| XAML page | `Views/<Screen>/View_<Feature>_<Screen>.(Windows\|Android).xaml` |
| Code-behind | `Views/<Screen>/View_<Feature>_<Screen>.xaml.cs` |
| ViewModel | `ViewModels/<Screen>/ViewModel_<Feature>_<Screen>.cs` |

The `<Screen>` subfolder name matches the `<Screen>` segment of the class name.