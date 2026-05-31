# FEATURE07-01: Research — Setup Technician Module + InforVisual ERP Integration

This file is the source of truth for all code facts gathered before implementing FEATURE-02 (InforVisual) and FEATURE-07 (Setup Technician Module). These two features are implemented together because FEATURE-07 requires every InforVisual query that FEATURE-02 defines. No further codebase searches are required to begin implementation.

---

## 1. Project Locations

### Client Solution (`MTM_Waitlist_Application.slnx`)

| Project | Path | Namespace | TFM |
|---------|------|-----------|-----|
| Core | `MTM_Waitlist_Application/Core/Core/` | `Core` | `net10.0` |
| Data | `MTM_Waitlist_Application/Core/Data/` | `Data` | `net10.0` |
| Services | `MTM_Waitlist_Application/Core/Services/` | `Services` | `net10.0` |
| Feature.Waitlist | `MTM_Waitlist_Application/Features/Feature.Waitlist/` | `Feature.Waitlist` | `net10.0` |
| Shared | `MTM_Waitlist_Application/MTM_Waitlist_Application/` | `MTM_Waitlist_Application` | `net10.0-windows10.0.19041.0` + `net10.0-android` |

### Server Solution (`MTM_Waitlist_Server.slnx`)

| Project | Path | Notes |
|---------|------|-------|
| Server Core | `MTM_Waitlist_Server/Core/` | Interfaces, models for server |
| Server Modules | `MTM_Waitlist_Server/Modules/` | Business logic modules |
| Server Admin host | `MTM_Waitlist_Server/Hosts/MTM_Waitlist_Server.Admin/` | WinUI 3 + in-process Kestrel REST API |

---

## 2. Confirmed Empty (Nothing Exists Yet)

The following paths **do not exist** — all must be created from scratch:

**Client — Core:**
- `Core/Models/InforVisual/` — no files
- `Core/Models/SetupTech/` — no files
- `Core/Interfaces/InforVisual/` — no files
- `Core/Interfaces/SetupTech/` — no files

**Client — Data:**
- `Data/Repositories/InforVisual/` — no files
- `Data/Repositories/SetupTech/` — no files

**Client — Services:**
- `Services/InforVisual/` — no files
- `Services/SetupTech/` — no files

**Client — Feature.Waitlist:**
- `Feature.Waitlist/Views/` — no files (the project is an empty stub with only a `Platforms/` folder)
- `Feature.Waitlist/ViewModels/` — no files

**Database:**
- `Database/infor_visual/` — no files
- `Database/schema/tables/SetupTech/` — no files
- `Database/procedures/SetupTech/` — no files
- `Database/triggers/SetupTech/` — no files
- `Database/indexes/SetupTech/` — no files

---

## 3. Existing Files That Will Be Modified

| File | Current State | What Changes |
|------|---------------|--------------|
| `MTM_Waitlist_Application/MauiProgramExtensions.cs` | Has `AddSharedServices()` with Auth + Dashboard + stubs for Waitlist/Mobile | Add InforVisual and SetupTech DI registrations; uncomment/add Waitlist page/VM registrations |
| `Features/Feature.Waitlist/Feature.Waitlist.csproj` | Empty stub — no Views, no NuGet packages beyond TFM | Add `CommunityToolkit.Mvvm 8.4.2` package; add platform XAML ItemGroups for 4 SetupTech screens |

---

## 4. Current `AddSharedServices()` Registration State

```csharp
// MTM_Waitlist_Application/MauiProgramExtensions.cs
// ── Infrastructure ────────────────────────────
services.AddSingleton<IConnectivity>(Connectivity.Current);
services.AddSingleton<IService_AuthTokenStore, Service_AuthTokenStore_Maui>();

// ── Data layer ────────────────────────────────
services.AddSingleton<LocalDbContext>();
services.AddSingleton<IApiClient, HttpApiClient>();
services.AddSingleton<IRepository_WaitlistEntry, Repository_WaitlistEntry>();
services.AddSingleton<IRepository_WaitlistEntryLocal, Repository_WaitlistEntryLocal>();

// ── Services ──────────────────────────────────
services.AddSingleton<IService_Auth, Service_Auth>();
services.AddSingleton<IService_WaitlistEntry, Service_WaitlistEntry>();
services.AddSingleton<ISyncService, SyncService>();
services.AddSingleton<IService_KillSwitch, Service_KillSwitch>();

// ── Feature: Auth ─────────────────────────────
services.AddTransient<ViewModel_Auth_Login>();
services.AddTransient<View_Auth_Login>();

// ── Feature: Dashboard ───────────────────────
services.AddTransient<ViewModel_Dashboard_Main>();
services.AddTransient<View_Dashboard_Main>();

// ── Feature: Waitlist ────────────────────────  (currently commented out — stub)
// services.AddTransient<ViewModel_Waitlist_Entry>();
// services.AddTransient<View_Waitlist_Entry>();
```

---

## 5. Existing Patterns to Follow

### Model_Dao_Result (from `Core/Models/Shared/`)

```csharp
// Already implemented — use this return type for ALL repository methods
Model_Dao_Result<T>.Success(T data)
Model_Dao_Result<T>.Failure(string errorMessage)
bool IsSuccess { get; }
T? Data { get; }
string? ErrorMessage { get; }
```

### Online Repository Pattern (from `Data/Repositories/Waitlist/Repository_WaitlistEntry.cs`)

```csharp
public sealed class Repository_WaitlistEntry : IRepository_WaitlistEntry
{
    private readonly IApiClient _apiClient;
    public Repository_WaitlistEntry(IApiClient apiClient) { _apiClient = apiClient; }
    public async Task<Model_Dao_Result<List<Model_WaitlistEntry>>> GetAllWaitlistEntriesAsync(
        CancellationToken cancellationToken = default)
        => await _apiClient.GetAsync<List<Model_WaitlistEntry>>(
               Constants_Api.WaitlistEntryEndpoint, cancellationToken);
}
```

### Local Repository Pattern (from `Data/Repositories/Waitlist/Repository_WaitlistEntryLocal.cs`)

```csharp
public sealed class Repository_WaitlistEntryLocal : IRepository_WaitlistEntryLocal
{
    private readonly LocalDbContext _context;
    public Repository_WaitlistEntryLocal(LocalDbContext context) { _context = context; }
    public async Task<Model_Dao_Result<List<Model_WaitlistEntry>>> GetAllWaitlistEntriesAsync()
    {
        try
        {
            await _context.InitializeAsync();
            var entities = await _context.Connection.Table<Entity_WaitlistEntry>().ToListAsync();
            return Model_Dao_Result<List<Model_WaitlistEntry>>.Success(
                entities.Select(MapToModel).ToList());
        }
        catch (Exception ex)
        {
            return Model_Dao_Result<List<Model_WaitlistEntry>>.Failure(
                $"Failed to retrieve local waitlist entries: {ex.Message}");
        }
    }
}
```

### Connectivity-Aware Service Pattern (from `Services/Waitlist/Service_WaitlistEntry.cs`)

```csharp
public sealed class Service_WaitlistEntry : IService_WaitlistEntry
{
    private readonly IConnectivity _connectivity;
    private readonly IRepository_WaitlistEntry _onlineRepository;
    private readonly IRepository_WaitlistEntryLocal _localRepository;
    private bool IsOnline => _connectivity.NetworkAccess == NetworkAccess.Internet;

    public async Task<Model_Dao_Result<List<Model_WaitlistEntry>>> GetAllEntriesAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsOnline)
        {
            var onlineResult = await _onlineRepository.GetAllWaitlistEntriesAsync(cancellationToken);
            if (onlineResult.IsSuccess) { return onlineResult; }
        }
        return await _localRepository.GetAllWaitlistEntriesAsync();
    }
}
```

### ViewModel Pattern

```csharp
public partial class ViewModel_Example : ObservableObject
{
    private readonly IService_X _service;
    [ObservableProperty] private ObservableCollection<Model_X> _items = [];
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _errorMessage = string.Empty;

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        var result = await _service.GetAllAsync(ct);
        if (result.IsSuccess) { Items = new ObservableCollection<Model_X>(result.Data!); }
        else { ErrorMessage = result.ErrorMessage ?? string.Empty; }
        IsBusy = false;
    }
}
```

---

## 6. InforVisual SQL Query Files (FEATURE-02)

Location in repo: `Database/infor_visual/queries/` (documentation / reference SQL, not executed by client)

These are the SQL scripts that the **server-side** REST API executes against `VISUAL\MTMFG`:

| File | Purpose | Key Tables |
|------|---------|-----------|
| `WL_01_GetWorkOrderHeader.sql` | Get WO header by `BASE_ID` | `WORK_ORDER`, `PART` |
| `WL_02_GetWorkOrderSequences.sql` | All sequences for a WO | `OPERATION`, `SHOP_RESOURCE` |
| `WL_03_GetActiveWorkOrdersForResource.sql` | Active WOs at a workcenter | `OPERATION`, `WORK_ORDER`, `PART` |
| `WL_04_GetAllWorkcenters.sql` | Full workcenter list | `SHOP_RESOURCE` |
| `WL_05_GetSubordinatePartsForWorkOrder.sql` | Component parts + on-hand qty | `WORK_ORDER_MATL`, `PART`, `PART_SITE` |

`WL_05` is new (added for FEATURE-07). Key join: `PART_SITE.PART_ID = WORK_ORDER_MATL.PART_ID AND PART_SITE.SITE_ID = '002'`.

---

## 7. Expected REST API Endpoints (Server Side — FEATURE-02 + FEATURE-07)

The client repositories call these via `IApiClient`. These endpoints must be added to `MTM_Waitlist_Server.Admin` (InforVisual and SetupTech controllers).

### InforVisual Endpoints

| HTTP | Route | Returns | Repository method |
|------|-------|---------|-------------------|
| GET | `/api/infor-visual/workcenters` | `List<Model_VisualWorkcenter>` | `GetAllWorkcentersAsync` |
| GET | `/api/infor-visual/work-orders/{workOrderId}` | `Model_VisualWorkOrderHeader` | `GetWorkOrderHeaderAsync` |
| GET | `/api/infor-visual/work-orders/{workOrderId}/sequences` | `List<Model_VisualWorkOrderSequence>` | `GetWorkOrderSequencesAsync` |
| GET | `/api/infor-visual/work-orders/{workOrderId}/sequences/{seqNo}/subordinate-parts` | `List<Model_Visual_SubordinatePart>` | `GetSubordinatePartsAsync` |
| GET | `/api/infor-visual/resources/{resourceId}/active-work-orders` | `List<Model_VisualWorkOrderHeader>` | `GetActiveWorkOrdersForResourceAsync` |

### SetupTech Endpoints

| HTTP | Route | Returns / Body | Repository method |
|------|-------|----------------|-------------------|
| GET | `/api/setup-tech/active-job/{workcenterId}` | `Model_SetupTech_ActiveJob?` | `GetActiveJobAsync` |
| POST | `/api/setup-tech/active-job` | body: `Model_SetupTech_ActiveJob` | `SetActiveJobAsync` |
| GET | `/api/setup-tech/dunnage-assignment/{workOrderId}/{seqNo}` | `List<Model_SetupTech_DunnageAssignment>` | `GetDunnageAssignmentAsync` |
| PUT | `/api/setup-tech/dunnage-assignment` | body: `Model_SetupTech_DunnageAssignment` | `UpsertDunnageAssignmentAsync` |
| DELETE | `/api/setup-tech/dunnage-assignment/{id}` | — | `DeleteDunnageAssignmentAsync` |
| GET | `/api/setup-tech/dunnage-types` | `List<Model_SetupTech_DunnageTypeConfig>` | `GetEnabledDunnageTypesAsync` |
| GET | `/api/setup-tech/job-history/{workcenterId}` | `List<Model_SetupTech_JobHistoryEntry>` | `GetJobHistoryAsync` |
| GET | `/api/dunnage-catalog` | `List<Model_DunnagePart>` | `GetDunageCatalogAsync` |
| GET | `/api/setup-tech/subordinate-parts/{workOrderId}/{seqNo}` | `List<Model_SetupTech_SubordinatePart>` | `GetCachedSubordinatePartsAsync` |

---

## 8. New `Constants_Api` Entries Needed

Add to `Core/Constants/Api/Constants_Api.cs`:

```csharp
// InforVisual endpoints
public const string VisualWorkcenters          = "api/infor-visual/workcenters";
public const string VisualWorkOrderHeader      = "api/infor-visual/work-orders/{0}";
public const string VisualWorkOrderSequences   = "api/infor-visual/work-orders/{0}/sequences";
public const string VisualSubordinateParts     = "api/infor-visual/work-orders/{0}/sequences/{1}/subordinate-parts";
public const string VisualActiveWorkOrders     = "api/infor-visual/resources/{0}/active-work-orders";

// SetupTech endpoints
public const string SetupTechActiveJob         = "api/setup-tech/active-job/{0}";
public const string SetupTechSetActiveJob      = "api/setup-tech/active-job";
public const string SetupTechDunnageAssignment = "api/setup-tech/dunnage-assignment/{0}/{1}";
public const string SetupTechUpsertDunnage     = "api/setup-tech/dunnage-assignment";
public const string SetupTechDeleteDunnage     = "api/setup-tech/dunnage-assignment/{0}";
public const string SetupTechDunnageTypes      = "api/setup-tech/dunnage-types";
public const string SetupTechJobHistory        = "api/setup-tech/job-history/{0}";
public const string DunageCatalog              = "api/dunnage-catalog";
public const string SetupTechSubordinateParts  = "api/setup-tech/subordinate-parts/{0}/{1}";
```

---

## 9. New Core Models — FEATURE-02 (InforVisual)

Location: `Core/Models/InforVisual/`

| File | Key Properties |
|------|---------------|
| `Model_VisualWorkOrderHeader.cs` | `WorkOrderId`, `PartId`, `PartDescription`, `WorkOrderStatus`, `DesiredQty`, `WantDate`, `SiteId` |
| `Model_VisualWorkOrderSequence.cs` | `WorkOrderId`, `SequenceNo`, `WorkcenterId`, `WorkcenterDescription`, `SequenceStatus`, `SetupCompleted`, `CompletedQty`, `TargetQty`, `SchedStart`, `SchedFinish` |
| `Model_VisualWorkcenter.cs` | `WorkcenterId`, `Description`, `ResourceType`, `DepartmentId`, `ScheduleGroup` |
| `Model_Visual_SubordinatePart.cs` | `WorkOrderId`, `SequenceNo`, `PartId`, `PartDescription`, `RequiredQty`, `QtyOnHand` |

**Work order status codes (Infor Visual):** `O` = Open, `R` = Released, `C` = Closed, `H` = Hold

---

## 10. New Core Models — FEATURE-07 (SetupTech)

Location: `Core/Models/SetupTech/`

| File | Key Properties |
|------|---------------|
| `Model_SetupTech_ActiveJob.cs` | `Id`, `WorkcenterId`, `WorkOrderId`, `SequenceNo`, `PartId`, `PartType`, `SubordinateParts`, `DunnageAssignments`, `SetupTechUserId`, `ActiveSince` |
| `Model_SetupTech_JobHistoryEntry.cs` | Same as ActiveJob + `ActiveFrom`, `ActiveUntil` |
| `Model_SetupTech_DunnageAssignment.cs` | `AssignmentId`, `WorkOrderId`, `SequenceNo`, `DunnagePartId`, `DunnagePartName`, `DunnageTypeId`, `DunnageTypeName`, `LastModifiedByUserId` — **no Quantity** |
| `Model_SetupTech_SubordinatePart.cs` | `WorkOrderId`, `SequenceNo`, `SubPartId`, `SubPartDesc`, `RequiredQty`, `QtyOnHand` |
| `Model_SetupTech_DunnageTypeConfig.cs` | `DunnageTypeId`, `DunnageTypeName`, `IsEnabled`, `DisplayOrder` |

---

## 11. New Core Interfaces — FEATURE-02 (InforVisual)

Location: `Core/Interfaces/InforVisual/`

### `IService_InforVisual.cs`

```csharp
public interface IService_InforVisual
{
    /// <summary>Returns all active workcenters. Cached after first call.</summary>
    Task<Model_Dao_Result<List<Model_VisualWorkcenter>>> GetAllWorkcentersAsync(
        CancellationToken ct = default);

    Task<Model_Dao_Result<Model_VisualWorkOrderHeader>> GetWorkOrderHeaderAsync(
        string workOrderId, CancellationToken ct = default);

    Task<Model_Dao_Result<List<Model_VisualWorkOrderSequence>>> GetWorkOrderSequencesAsync(
        string workOrderId, CancellationToken ct = default);

    Task<Model_Dao_Result<List<Model_Visual_SubordinatePart>>> GetSubordinatePartsAsync(
        string workOrderId, int sequenceNo, CancellationToken ct = default);

    Task<Model_Dao_Result<List<Model_VisualWorkOrderHeader>>> GetActiveWorkOrdersForResourceAsync(
        string resourceId, CancellationToken ct = default);

    /// <summary>Forces a refresh of the workcenter cache on next call.</summary>
    void InvalidateWorkcenterCache();
}
```

### Repository Interfaces: `IRepository_InforVisual.cs` (online) + `IRepository_InforVisualLocal.cs` (offline cache)

`IRepository_InforVisualLocal` caches only the workcenter list (the one piece of data that is large and rarely changes). Work order data is never cached locally — always requires a network call.

---

## 12. New Core Interfaces — FEATURE-07 (SetupTech)

Location: `Core/Interfaces/SetupTech/`

| Interface | Purpose |
|-----------|---------|
| `IService_SetupTech.cs` | Connectivity-aware service; delegates to online or local repos |
| `IRepository_SetupTechActiveJob.cs` | Online: get/set active job via REST API |
| `IRepository_SetupTechActiveJobLocal.cs` | Offline: sqlite-net-pcl cache of last known active job per workcenter |
| `IRepository_WorkOrderDunnage.cs` | Online: CRUD for dunnage assignments |
| `IRepository_WorkOrderDunnageLocal.cs` | Offline: sqlite-net-pcl cache of dunnage assignments |
| `IRepository_SetupTechDunnageTypeConfig.cs` | Online: get enabled dunnage type rows |
| `IRepository_SetupTechDunnageTypeConfigLocal.cs` | Offline: sqlite-net-pcl cache of enabled types |

---

## 13. New Database Tables (MySQL `mtm_waitlist_application`)

All 5 tables are new. Full DDL is in `FEATURE-07-Setup-Technician-Module.md`. Summary:

| Table | Primary Key | Notes |
|-------|-------------|-------|
| `WorkstationActiveJobs` | `Id` | UNIQUE on `WorkcenterId` — one active job per workcenter |
| `WorkstationJobHistory` | `Id` | Archive of previous active jobs |
| `WorkOrderDunnageAssignments` | `Id` | UNIQUE on `(WorkOrderId, SequenceNo, DunnagePartId)` — no Quantity |
| `SetupTechDunnageTypeConfig` | `Id` | UNIQUE on `DunnageTypeId` — mirrors receiving app dunnage_types |
| `WorkOrderSubordinateParts` | `Id` | UNIQUE on `(WorkOrderId, SequenceNo, SubPartId)` — cache from Infor Visual |

**Migration file:** `Database/migrations/V002__SetupTech_Schema.sql`

---

## 14. New Stored Procedures (MySQL `mtm_waitlist_application`)

Location: `Database/procedures/SetupTech/`

| Procedure | Table(s) Touched |
|-----------|-----------------|
| `usp_SetupTech_GetActiveJob` | `WorkstationActiveJobs` |
| `usp_SetupTech_SetActiveJob` | `WorkstationActiveJobs` → `WorkstationJobHistory` (archive + insert new) |
| `usp_SetupTech_GetDunnageAssignment` | `WorkOrderDunnageAssignments` |
| `usp_SetupTech_UpsertDunnageAssignment` | `WorkOrderDunnageAssignments` |
| `usp_SetupTech_DeleteDunnageAssignment` | `WorkOrderDunnageAssignments` |
| `usp_SetupTech_UpsertSubordinateParts` | `WorkOrderSubordinateParts` |
| `usp_SetupTech_GetJobHistory` | `WorkstationJobHistory` |
| `usp_SetupTech_GetEnabledDunnageTypes` | `SetupTechDunnageTypeConfig` WHERE `IsEnabled = 1` ORDER BY `DisplayOrder` |
| `usp_SetupTech_UpsertDunnageTypeConfig` | `SetupTechDunnageTypeConfig` |

---

## 15. New Triggers (UpdatedAt automation)

Location: `Database/triggers/SetupTech/`

Each new table needs a `BEFORE UPDATE` trigger to set `UpdatedAt = UTC_TIMESTAMP()`:

| Trigger file | Timing + Event |
|---|---|
| `trg_WorkstationActiveJobs_BeforeUpdate.sql` | `BEFORE UPDATE ON WorkstationActiveJobs` |
| `trg_WorkstationJobHistory_BeforeUpdate.sql` | `BEFORE UPDATE ON WorkstationJobHistory` |
| `trg_WorkOrderDunnageAssignments_BeforeUpdate.sql` | `BEFORE UPDATE ON WorkOrderDunnageAssignments` |
| `trg_SetupTechDunnageTypeConfig_BeforeUpdate.sql` | `BEFORE UPDATE ON SetupTechDunnageTypeConfig` |
| `trg_WorkOrderSubordinateParts_BeforeUpdate.sql` | `BEFORE UPDATE ON WorkOrderSubordinateParts` |

---

## 16. New Indexes

Location: `Database/indexes/SetupTech/`

Files: `WorkstationActiveJobs_Indexes.sql`, `WorkstationJobHistory_Indexes.sql`, `WorkOrderDunnageAssignments_Indexes.sql`, `SetupTechDunnageTypeConfig_Indexes.sql`, `WorkOrderSubordinateParts_Indexes.sql`

All FK columns get `idx_<Table>_<Column>` covering indexes. Additionally:
- `WorkstationJobHistory`: index on `(WorkcenterId, ActiveFrom)` for analytics queries
- `WorkOrderDunnageAssignments`: index on `(WorkOrderId, SequenceNo)` for lookup
- `WorkOrderSubordinateParts`: index on `(WorkOrderId, SequenceNo)` for lookup

---

## 17. Feature.Waitlist.csproj — Required Changes

The `.csproj` currently is an empty stub. It needs:

1. **NuGet package:** `CommunityToolkit.Mvvm 8.4.2`
2. **NoWarn:** `MVVMTK0045` (CommunityToolkit source generator warning)
3. **Platform XAML ItemGroups** for each of the 4 SetupTech screens (see `platform-xaml.instructions.md` for exact pattern):
   - `View_Waitlist_SetupTech` (steps 1 & 2)
   - `View_Waitlist_SetupTechValidation` (step 3)
   - `View_Waitlist_SetupTechDunnage` (step 4)
   - `View_Waitlist_SetupTechConfirmation` (step 5)

---

## 18. ViewModels to Create

Location: `Feature.Waitlist/ViewModels/<Screen>/`

| File | Screen | Key Observable Properties | Key Commands |
|------|--------|--------------------------|--------------|
| `ViewModel_Waitlist_SetupTech.cs` | Steps 1 & 2 | `AvailableWorkcenters`, `SelectedWorkcenter`, `WorkOrderInput`, `SequenceInput`, `AvailableSequences`, `WizardStep (1–6)`, `IsBusy`, `ErrorMessage` | `LoadWorkcentersAsync`, `SelectWorkcenterAsync`, `ScanBarcodeAsync`, `LookupWorkOrderAsync`, `LoadSequencesAsync`, `ApproveWorkOrderAsync`, `NavigateToDunnageAsync`, `SaveActiveJobAsync` |
| `ViewModel_Waitlist_SetupTechValidation.cs` | Step 3 | `WorkOrderHeader`, `SubordinateParts`, `SelectedSequence`, `IsLoading` | `ApproveAsync`, `ChangeWorkOrderAsync` |
| `ViewModel_Waitlist_SetupTechDunnage.cs` | Step 4 | `CurrentAssignments`, `DunnageCatalog`, `EnabledDunnageTypes`, `SelectedTypeFilter`, `IsSaving` | `LoadAssignmentAsync`, `AddDunnageItemAsync`, `RemoveDunnageItemAsync`, `SaveAndContinueAsync` |
| `ViewModel_Waitlist_SetupTechConfirmation.cs` | Step 5 | `ActiveJob`, `IsSaving`, `SaveSucceeded`, `ErrorMessage` | `ConfirmSaveAsync`, `StartOverAsync` |

---

## 19. Views to Create

Location: `Feature.Waitlist/Views/<Screen>/`

| Subfolder | Windows XAML | Android XAML | Code-Behind |
|-----------|-------------|-------------|-------------|
| `SetupTech/` | `View_Waitlist_SetupTech.Windows.xaml` | `View_Waitlist_SetupTech.Android.xaml` | `View_Waitlist_SetupTech.xaml.cs` |
| `SetupTechValidation/` | `View_Waitlist_SetupTechValidation.Windows.xaml` | `View_Waitlist_SetupTechValidation.Android.xaml` | `View_Waitlist_SetupTechValidation.xaml.cs` |
| `SetupTechDunnage/` | `View_Waitlist_SetupTechDunnage.Windows.xaml` | `View_Waitlist_SetupTechDunnage.Android.xaml` | `View_Waitlist_SetupTechDunnage.xaml.cs` |
| `SetupTechConfirmation/` | `View_Waitlist_SetupTechConfirmation.Windows.xaml` | `View_Waitlist_SetupTechConfirmation.Android.xaml` | `View_Waitlist_SetupTechConfirmation.xaml.cs` |

**Windows XAML:** `Microsoft.UI.Xaml` namespace, `Page` (not ContentPage), `{x:Bind ViewModel.Property, Mode=OneWay}`, typed `ViewModel` property on code-behind.

**Android XAML:** MAUI `ContentPage`, `x:DataType`, compiled bindings `{Binding Property, Mode=OneWay}`, minimum 48 px tap targets.

---

## 20. DI Registrations to Add

```csharp
// ── InforVisual (FEATURE-02) ────────────────────────────────────────────────
services.AddSingleton<IRepository_InforVisual, Repository_InforVisual>();
services.AddSingleton<IRepository_InforVisualLocal, Repository_InforVisualLocal>();
services.AddSingleton<IService_InforVisual, Service_InforVisual>();

// ── SetupTech — Data layer (FEATURE-07) ────────────────────────────────────
services.AddSingleton<IRepository_SetupTechActiveJob, Repository_SetupTechActiveJob>();
services.AddSingleton<IRepository_SetupTechActiveJobLocal, Repository_SetupTechActiveJobLocal>();
services.AddSingleton<IRepository_WorkOrderDunnage, Repository_WorkOrderDunnage>();
services.AddSingleton<IRepository_WorkOrderDunnageLocal, Repository_WorkOrderDunnageLocal>();
services.AddSingleton<IRepository_SetupTechDunnageTypeConfig, Repository_SetupTechDunnageTypeConfig>();
services.AddSingleton<IRepository_SetupTechDunnageTypeConfigLocal, Repository_SetupTechDunnageTypeConfigLocal>();

// ── SetupTech — Service (FEATURE-07) ───────────────────────────────────────
services.AddSingleton<IService_SetupTech, Service_SetupTech>();

// ── Feature: Waitlist — SetupTech ViewModels + Pages (FEATURE-07) ──────────
services.AddTransient<ViewModel_Waitlist_SetupTech>();
services.AddTransient<View_Waitlist_SetupTech>();
services.AddTransient<ViewModel_Waitlist_SetupTechValidation>();
services.AddTransient<View_Waitlist_SetupTechValidation>();
services.AddTransient<ViewModel_Waitlist_SetupTechDunnage>();
services.AddTransient<View_Waitlist_SetupTechDunnage>();
services.AddTransient<ViewModel_Waitlist_SetupTechConfirmation>();
services.AddTransient<View_Waitlist_SetupTechConfirmation>();
```

---

## 21. Role Guard Logic

```csharp
// In ViewModel_Waitlist_SetupTech constructor or OnNavigatedToAsync
if (!_authService.CurrentUser.HasRole(Enum_UserRole.SetupTech) &&
    !_authService.CurrentUser.HasRole(Enum_UserRole.Admin) &&
    !_authService.CurrentUser.HasRole(Enum_UserRole.Developer))
{
    // Windows: raise NavigateBackRequested event — Page calls Frame.GoBack()
    // Android: call Shell.Current.GoToAsync("..")
    _navigationService.GoBack();
    return;
}
```

`Enum_UserRole` already exists in `Core/Enums/`. `SetupTech` role is already seeded in `Users.sql`.

---

## 22. Seed Data Required

`Database/seed/03_Seed_SetupTechDunnageTypeConfig.sql` — insert `SetupTechDunnageTypeConfig` rows with:
- **Enabled** (IsEnabled = 1): type IDs 1 (Pallets/Skids), 2 (Cardboard), 3 (Corrugated Boxes), 4 (Gaylords), 12 (Returnable Totes), 13 (Returnable Baskets/Wire Containers)
- **Disabled** (IsEnabled = 0): type IDs 5–11

---

## 23. Open Questions (From Feature Spec)

These are unresolved and must be confirmed with Dan/Nick before implementing the server-side API filters:

| Question | Impact |
|----------|--------|
| `SCHEDULE_NORMALLY = 'Y'` filter for workcenters — is this correct? | `WL_04_GetAllWorkcenters.sql` WHERE clause |
| Which `WORK_ORDER.STATUS` values are "active"? (`O`, `R`, `H`?) | `WL_03` + validation in validation screen |
| `SITE_ID` value for subordinate part on-hand lookup (`'002'` assumed) | `WL_05` WHERE clause |
| Domain-joined vs. non-domain-joined kiosk SQL auth | `Server=VISUAL` connection string |
| Dunnage assignments — global per WO/Seq or per-user? (Current spec: global) | `WorkOrderDunnageAssignments` FK |
| Subordinate part cache invalidation — time-based or on next setup? | `Repository_SetupTechActiveJobLocal` eviction logic |

---

## 24. Required Test Expansion

The implementation scope now explicitly includes automated coverage for both the server SQL assets and the client-to-server communication path.

### Server-side SQL test home

| Area | Project | Suggested path |
|------|---------|----------------|
| SetupTech migration/schema/procedure/seed tests | `MTM_Waitlist_Server.Api.Tests` | `MTM_Waitlist_Server/Tests/MTM_Waitlist_Server.Api.Tests/Database/SetupTech/` |
| InforVisual query-file and DAO query coverage | `MTM_Waitlist_Server.Api.Tests` | `MTM_Waitlist_Server/Tests/MTM_Waitlist_Server.Api.Tests/Database/InforVisual/` |

Every SQL file created for FEATURE-07 in `MTM_Waitlist_Server/Database/` must have at least one corresponding automated test in the server API test project.

### Waitlist app talking to the server app

| Platform | Project | Suggested path |
|----------|---------|----------------|
| WinUI | `MTM_Waitlist_Application.UITests.WinUI` | `MTM_Waitlist_Application/Tests/UI/MTM_Waitlist_Application.UITests.WinUI/Flows/` |
| Droid | `MTM_Waitlist_Application.UITests.Droid` | `MTM_Waitlist_Application/Tests/UI/MTM_Waitlist_Application.UITests.Droid/Flows/` |

Minimum required platform coverage:
- successful authentication against the running server app
- workcenter lookup from the server-side InforVisual endpoint
- work-order + sequence lookup from the server app
- dunnage configuration/catalog retrieval from the server app
- active-job save posted successfully to the server app
- graceful failure messaging when the server app is unavailable
