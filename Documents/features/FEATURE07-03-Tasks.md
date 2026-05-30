# FEATURE07-03: Tasks — Setup Technician Module + InforVisual ERP Integration Checklist

Check off items as you complete them. All items start unchecked. Reference [FEATURE07-02-Plan.md](FEATURE07-02-Plan.md) for detailed implementation notes on each phase.

---

## Phase 1 — Database Schema

### 1.1 Table DDL Files (`Database/schema/tables/SetupTech/`)

| Done | Task | File |
|------|------|------|
| [ ] | Create `WorkstationActiveJobs.sql` | UNIQUE on `WorkcenterId`, FK to `Users`, `ActiveSince` + `CreatedAt` + `UpdatedAt` |
| [ ] | Create `WorkstationJobHistory.sql` | `ActiveFrom` + `ActiveUntil`, FK to `Users` |
| [ ] | Create `WorkOrderDunnageAssignments.sql` | UNIQUE on `(WorkOrderId, SequenceNo, DunnagePartId)`, no Quantity column, FK to `Users` |
| [ ] | Create `SetupTechDunnageTypeConfig.sql` | UNIQUE on `DunnageTypeId`, `IsEnabled TINYINT(1)`, `DisplayOrder INT UNSIGNED` |
| [ ] | Create `WorkOrderSubordinateParts.sql` | UNIQUE on `(WorkOrderId, SequenceNo, SubPartId)`, `QtyOnHand DECIMAL(10,4)`, `CachedAt DATETIME` |

### 1.2 Index Files (`Database/indexes/SetupTech/`)

| Done | Task | File |
|------|------|------|
| [ ] | Create `WorkstationActiveJobs_Indexes.sql` | `idx_WorkstationActiveJobs_Workcenter` + `idx_WorkstationActiveJobs_User` |
| [ ] | Create `WorkstationJobHistory_Indexes.sql` | `idx_WorkstationJobHistory_Workcenter` + `idx_WorkstationJobHistory_WorkcenterFrom (WorkcenterId, ActiveFrom)` |
| [ ] | Create `WorkOrderDunnageAssignments_Indexes.sql` | `idx_WODunnage_WOSeq (WorkOrderId, SequenceNo)` + `idx_WODunnage_User` |
| [ ] | Create `SetupTechDunnageTypeConfig_Indexes.sql` | `idx_SetupTechDunnageTypeConfig_DisplayOrder` |
| [ ] | Create `WorkOrderSubordinateParts_Indexes.sql` | `idx_WOSubParts_WOSeq (WorkOrderId, SequenceNo)` |

### 1.3 Trigger Files (`Database/triggers/SetupTech/`)

| Done | Task | File |
|------|------|------|
| [ ] | Create `trg_WorkstationActiveJobs_BeforeUpdate.sql` | `SET NEW.UpdatedAt = UTC_TIMESTAMP()` |
| [ ] | Create `trg_WorkstationJobHistory_BeforeUpdate.sql` | `SET NEW.UpdatedAt = UTC_TIMESTAMP()` |
| [ ] | Create `trg_WorkOrderDunnageAssignments_BeforeUpdate.sql` | `SET NEW.UpdatedAt = UTC_TIMESTAMP()` |
| [ ] | Create `trg_SetupTechDunnageTypeConfig_BeforeUpdate.sql` | `SET NEW.UpdatedAt = UTC_TIMESTAMP()` |
| [ ] | Create `trg_WorkOrderSubordinateParts_BeforeUpdate.sql` | `SET NEW.UpdatedAt = UTC_TIMESTAMP()` |

### 1.4 Stored Procedure Files (`Database/procedures/SetupTech/`)

| Done | Task | File |
|------|------|------|
| [ ] | Create `usp_SetupTech_GetActiveJob.sql` | SELECT by `WorkcenterId`; `DROP PROCEDURE IF EXISTS` before CREATE |
| [ ] | Create `usp_SetupTech_SetActiveJob.sql` | Transaction: archive existing → DELETE → INSERT new; `EXIT HANDLER + ROLLBACK + RESIGNAL` |
| [ ] | Create `usp_SetupTech_GetDunnageAssignment.sql` | SELECT by `WorkOrderId + SequenceNo` |
| [ ] | Create `usp_SetupTech_UpsertDunnageAssignment.sql` | `INSERT … ON DUPLICATE KEY UPDATE` |
| [ ] | Create `usp_SetupTech_DeleteDunnageAssignment.sql` | DELETE by `Id` |
| [ ] | Create `usp_SetupTech_UpsertSubordinateParts.sql` | Batch upsert; `INSERT … ON DUPLICATE KEY UPDATE` |
| [ ] | Create `usp_SetupTech_GetJobHistory.sql` | SELECT by `WorkcenterId` ORDER BY `ActiveFrom DESC`, LIMIT `@PageSize` |
| [ ] | Create `usp_SetupTech_GetEnabledDunnageTypes.sql` | SELECT WHERE `IsEnabled = 1` ORDER BY `DisplayOrder` |
| [ ] | Create `usp_SetupTech_UpsertDunnageTypeConfig.sql` | `INSERT … ON DUPLICATE KEY UPDATE` |

### 1.5 Migration File

| Done | Task | File |
|------|------|------|
| [ ] | Create `Database/migrations/V002__SetupTech_Schema.sql` | Runs all 5 table DDLs in dependency order; then indexes, triggers, procedures; re-runnable |

### 1.6 Seed File

| Done | Task | File |
|------|------|------|
| [ ] | Create `Database/seed/03_Seed_SetupTechDunnageTypeConfig.sql` | 13 rows; types 1–4 and 12–13 enabled; types 5–11 disabled |

---

## Phase 2 — InforVisual SQL Query Files (`Database/infor_visual/queries/`)

| Done | Task | File |
|------|------|------|
| [ ] | Create `WL_01_GetWorkOrderHeader.sql` | `WORK_ORDER` JOIN `PART` WHERE `BASE_ID = @WorkOrderBaseId AND TYPE = 'W'` |
| [ ] | Create `WL_02_GetWorkOrderSequences.sql` | `OPERATION` JOIN `SHOP_RESOURCE` ORDER BY `SEQUENCE_NO` |
| [ ] | Create `WL_03_GetActiveWorkOrdersForResource.sql` | Filter `op.STATUS IN ('O','R','S')` AND `wo.STATUS IN ('O','R')` |
| [ ] | Create `WL_04_GetAllWorkcenters.sql` | `SHOP_RESOURCE WHERE SCHEDULE_NORMALLY = 'Y'` — **confirm filter with Dan before merge** |
| [ ] | Create `WL_05_GetSubordinatePartsForWorkOrder.sql` | `WORK_ORDER_MATL` JOIN `PART` LEFT JOIN `PART_SITE` WHERE `SITE_ID = '002'` — **confirm SITE_ID with Dan** |

---

## Phase 3 — Server-Side: InforVisual + SetupTech API Endpoints (`MTM_Waitlist_Server`)

| Done | Task | Details |
|------|------|---------|
| [ ] | Add InforVisual connection string to server appsettings | `"Server=VISUAL;Database=MTMFG;Integrated Security=True;ApplicationIntent=ReadOnly;Connect Timeout=5;Command Timeout=10;"` |
| [ ] | Create server-side `Dao_InforVisualWorkOrder.cs` | 5 methods; SQL Server via `Microsoft.Data.SqlClient`; all return `Model_Dao_Result<T>`; never throws |
| [ ] | Create `InforVisualController.cs` | Route `api/infor-visual`; 5 GET endpoints; 503 when DAO returns failure |
| [ ] | Create `SetupTechController.cs` | Route `api/setup-tech`; GET/POST/PUT/DELETE endpoints per Research §7 |
| [ ] | Add `GET /api/dunnage-catalog` endpoint | Cross-database read from `mtm_receiving_application.dunnage_parts JOIN dunnage_types` |
| [ ] | Register `Dao_InforVisualWorkOrder` in server DI | Singleton; inject connection string from configuration |
| [ ] | Build server solution — zero errors | `dotnet build MTM_Waitlist_Server.slnx` |

---

## Phase 4 — Client Core Models: InforVisual (`Core/Models/InforVisual/`)

| Done | Task | File |
|------|------|------|
| [ ] | Create `Model_VisualWorkOrderHeader.cs` | Properties: `WorkOrderId`, `PartId`, `PartDescription`, `WorkOrderStatus`, `DesiredQty`, `WantDate`, `SiteId`; XML doc on class |
| [ ] | Create `Model_VisualWorkOrderSequence.cs` | Properties: `WorkOrderId`, `SequenceNo`, `WorkcenterId`, `WorkcenterDescription`, `SequenceStatus`, `SetupCompleted`, `CompletedQty`, `TargetQty`, `SchedStart`, `SchedFinish` |
| [ ] | Create `Model_VisualWorkcenter.cs` | Properties: `WorkcenterId`, `Description`, `ResourceType`, `DepartmentId`, `ScheduleGroup` |
| [ ] | Create `Model_Visual_SubordinatePart.cs` | Properties: `WorkOrderId`, `SequenceNo`, `PartId`, `PartDescription`, `RequiredQty`, `QtyOnHand` |

---

## Phase 5 — Client Core Models: SetupTech (`Core/Models/SetupTech/`)

| Done | Task | File |
|------|------|------|
| [ ] | Create `Model_SetupTech_ActiveJob.cs` | `Id`, `WorkcenterId`, `WorkOrderId`, `SequenceNo`, `PartId`, `PartType`, `SubordinateParts`, `DunnageAssignments`, `SetupTechUserId`, `ActiveSince` |
| [ ] | Create `Model_SetupTech_JobHistoryEntry.cs` | Same as ActiveJob + `ActiveFrom`, `ActiveUntil` |
| [ ] | Create `Model_SetupTech_DunnageAssignment.cs` | `AssignmentId`, `WorkOrderId`, `SequenceNo`, `DunnagePartId`, `DunnagePartName`, `DunnageTypeId`, `DunnageTypeName`, `LastModifiedByUserId` — **NO Quantity property** |
| [ ] | Create `Model_SetupTech_SubordinatePart.cs` | `WorkOrderId`, `SequenceNo`, `SubPartId`, `SubPartDesc`, `RequiredQty`, `QtyOnHand` |
| [ ] | Create `Model_SetupTech_DunnageTypeConfig.cs` | `DunnageTypeId`, `DunnageTypeName`, `IsEnabled`, `DisplayOrder` |

---

## Phase 6 — Client Core Constants

| Done | Task | Details |
|------|------|---------|
| [ ] | Add InforVisual constants to `Constants_Api.cs` | 5 entries: `VisualWorkcenters`, `VisualWorkOrderHeader`, `VisualWorkOrderSequences`, `VisualSubordinateParts`, `VisualActiveWorkOrders` |
| [ ] | Add SetupTech constants to `Constants_Api.cs` | 9 entries: `SetupTechActiveJob`, `SetupTechSetActiveJob`, `SetupTechDunnageAssignment`, `SetupTechUpsertDunnage`, `SetupTechDeleteDunnage`, `SetupTechDunnageTypes`, `SetupTechJobHistory`, `DunageCatalog`, `SetupTechSubordinateParts` |

---

## Phase 7 — Client Core Interfaces

### InforVisual (`Core/Interfaces/InforVisual/`)

| Done | Task | File |
|------|------|------|
| [ ] | Create `IService_InforVisual.cs` | 5 query methods + `InvalidateWorkcenterCache()` — see Research §11 for full signature |
| [ ] | Create `IRepository_InforVisual.cs` | Same 5 query methods; no caching |
| [ ] | Create `IRepository_InforVisualLocal.cs` | `GetCachedWorkcentersAsync()` + `SaveWorkcentersAsync(List<Model_VisualWorkcenter>)` only |

### SetupTech (`Core/Interfaces/SetupTech/`)

| Done | Task | File |
|------|------|------|
| [ ] | Create `IService_SetupTech.cs` | All user-facing operations mirroring repository methods |
| [ ] | Create `IRepository_SetupTechActiveJob.cs` | `GetActiveJobAsync(string workcenterId)`, `SetActiveJobAsync(Model_SetupTech_ActiveJob)` |
| [ ] | Create `IRepository_SetupTechActiveJobLocal.cs` | Same signatures; sqlite-net-pcl |
| [ ] | Create `IRepository_WorkOrderDunnage.cs` | Get/Upsert/Delete dunnage assignments |
| [ ] | Create `IRepository_WorkOrderDunnageLocal.cs` | Same; sqlite-net-pcl |
| [ ] | Create `IRepository_SetupTechDunnageTypeConfig.cs` | `GetEnabledDunnageTypesAsync()` |
| [ ] | Create `IRepository_SetupTechDunnageTypeConfigLocal.cs` | `GetCachedDunnageTypesAsync()` + `SaveDunnageTypesAsync()` |

---

## Phase 8 — Client Data Repositories: InforVisual (`Data/Repositories/InforVisual/`)

| Done | Task | File |
|------|------|------|
| [ ] | Create `Repository_InforVisual.cs` | Implements `IRepository_InforVisual`; ctor takes `IApiClient`; all 5 methods delegate to `_apiClient.GetAsync<T>` |
| [ ] | Create `Repository_InforVisualLocal.cs` | Implements `IRepository_InforVisualLocal`; ctor takes `LocalDbContext`; workcenter cache only via `sqlite-net-pcl` |
| [ ] | Create `Entity_VisualWorkcenter.cs` (sqlite-net-pcl entity) | `[Table("VisualWorkcenterCache")]`; mirrors `Model_VisualWorkcenter` properties |

---

## Phase 9 — Client Data Repositories: SetupTech (`Data/Repositories/SetupTech/`)

| Done | Task | File |
|------|------|------|
| [ ] | Create `Repository_SetupTechActiveJob.cs` | Online; ctor takes `IApiClient` |
| [ ] | Create `Repository_SetupTechActiveJobLocal.cs` | Local; ctor takes `LocalDbContext` |
| [ ] | Create `Repository_WorkOrderDunnage.cs` | Online; ctor takes `IApiClient` |
| [ ] | Create `Repository_WorkOrderDunnageLocal.cs` | Local; ctor takes `LocalDbContext` |
| [ ] | Create `Repository_SetupTechDunnageTypeConfig.cs` | Online; ctor takes `IApiClient` |
| [ ] | Create `Repository_SetupTechDunnageTypeConfigLocal.cs` | Local; ctor takes `LocalDbContext` |
| [ ] | All local repos: wrap in try/catch, return `Model_Dao_Result.Failure` on exception | Verify pattern matches existing `Repository_WaitlistEntryLocal` |

---

## Phase 10 — Client Services

### `Service_InforVisual` (`Services/InforVisual/Service_InforVisual.cs`)

| Done | Task | Details |
|------|------|---------|
| [ ] | Create `Service_InforVisual.cs` skeleton | `sealed class`, constructor injection: `IConnectivity`, `IRepository_InforVisual`, `IRepository_InforVisualLocal` |
| [ ] | Implement workcenter cache | Private `List<Model_VisualWorkcenter>? _workcenterCache` field; `GetAllWorkcentersAsync` returns cache if not null; populates and saves to local on first call |
| [ ] | Implement `InvalidateWorkcenterCache()` | Sets `_workcenterCache = null` |
| [ ] | Implement WO header, sequences, subordinate parts, active WOs methods | Online only; offline → `Model_Dao_Result.Failure("Infor Visual is not available while offline.")` |
| [ ] | Implement offline workcenter fallback | If online fails or offline → try `_localRepository.GetCachedWorkcentersAsync()` |

### `Service_SetupTech` (`Services/SetupTech/Service_SetupTech.cs`)

| Done | Task | Details |
|------|------|---------|
| [ ] | Create `Service_SetupTech.cs` skeleton | `sealed class`, constructor injection: `IConnectivity`, all 6 repository interfaces |
| [ ] | Implement `GetActiveJobAsync` | Online → online repo; offline → local repo |
| [ ] | Implement `SetActiveJobAsync` | **Online only** — return failure if offline; no local write |
| [ ] | Implement `GetDunnageAssignmentAsync` | Online → online repo; offline → local repo |
| [ ] | Implement `UpsertDunnageAssignmentAsync` | Online only; return failure if offline |
| [ ] | Implement `DeleteDunnageAssignmentAsync` | Online only; return failure if offline |
| [ ] | Implement `GetEnabledDunnageTypesAsync` | Online → online repo + sync to local; offline → local repo |
| [ ] | Implement `GetCachedSubordinatePartsAsync` | Online → online repo; offline → local repo |

---

## Phase 11 — Client ViewModels

### `ViewModel_Waitlist_SetupTech` (`Feature.Waitlist/ViewModels/SetupTech/`)

| Done | Task | Details |
|------|------|---------|
| [ ] | Create `ViewModel_Waitlist_SetupTech.cs` | `partial class`, inherits `ObservableObject`, inject `IService_InforVisual`, `IService_SetupTech`, `IService_Auth` |
| [ ] | Add role guard | Check `SetupTech OR Admin OR Developer`; raise `NavigateBackRequested` event if unauthorized |
| [ ] | Add `[ObservableProperty] AvailableWorkcenters` | `ObservableCollection<Model_VisualWorkcenter>` |
| [ ] | Add `[ObservableProperty] SelectedWorkcenter` | `Model_VisualWorkcenter?` |
| [ ] | Add `[ObservableProperty] WorkOrderInput` | `string` |
| [ ] | Add `[ObservableProperty] SequenceInput` | `int?` |
| [ ] | Add `[ObservableProperty] AvailableSequences` | `ObservableCollection<Model_VisualWorkOrderSequence>` |
| [ ] | Add `[ObservableProperty] WorkOrderHeader` | `Model_VisualWorkOrderHeader?` |
| [ ] | Add `[ObservableProperty] SubordinateParts` | `ObservableCollection<Model_Visual_SubordinatePart>` |
| [ ] | Add `[ObservableProperty] WizardStep` | `int` (1–2 for steps in this view) |
| [ ] | Add `[ObservableProperty] IsBusy`, `IsVisualOffline`, `ErrorMessage` | Standard busy/error state |
| [ ] | Implement `[RelayCommand] LoadWorkcentersAsync` | Calls `IService_InforVisual.GetAllWorkcentersAsync` |
| [ ] | Implement `[RelayCommand] SelectWorkcenterAsync` | Sets `SelectedWorkcenter`, advances `WizardStep` to 2 |
| [ ] | Implement `[RelayCommand] ScanBarcodeAsync` | Parses `{WorkOrderId}:{SequenceNo}` barcode string into `WorkOrderInput` + `SequenceInput` |
| [ ] | Implement `[RelayCommand] LookupWorkOrderAsync` | Calls `GetWorkOrderHeaderAsync` + `GetWorkOrderSequencesAsync`; on success raises navigation event to validation screen |
| [ ] | Implement `[RelayCommand] LoadSequencesAsync` | Calls `GetWorkOrderSequencesAsync` to populate `AvailableSequences` |
| [ ] | Add `NavigateToValidationRequested` event | `event Action<Model_VisualWorkOrderHeader, IList<Model_Visual_SubordinatePart>, Model_VisualWorkOrderSequence>?` |

### `ViewModel_Waitlist_SetupTechValidation` (`Feature.Waitlist/ViewModels/SetupTechValidation/`)

| Done | Task | Details |
|------|------|---------|
| [ ] | Create `ViewModel_Waitlist_SetupTechValidation.cs` | `partial class`, inherits `ObservableObject` |
| [ ] | Add `[ObservableProperty] WorkOrderHeader`, `SubordinateParts`, `SelectedSequence` | Populated by navigation parameter |
| [ ] | Add `[ObservableProperty] IsLoading`, `ErrorMessage` | |
| [ ] | Implement `[RelayCommand] ApproveAsync` | Raises `NavigateToDunnageRequested` event |
| [ ] | Implement `[RelayCommand] ChangeWorkOrderAsync` | Raises `NavigateBackRequested` event |

### `ViewModel_Waitlist_SetupTechDunnage` (`Feature.Waitlist/ViewModels/SetupTechDunnage/`)

| Done | Task | Details |
|------|------|---------|
| [ ] | Create `ViewModel_Waitlist_SetupTechDunnage.cs` | `partial class`, inherits `ObservableObject`, inject `IService_SetupTech` |
| [ ] | Add `[ObservableProperty] CurrentAssignments` | `ObservableCollection<Model_SetupTech_DunnageAssignment>` |
| [ ] | Add `[ObservableProperty] DunnageCatalog` | `ObservableCollection<Model_DunnagePart>` (filtered by `SelectedTypeFilter`) |
| [ ] | Add `[ObservableProperty] EnabledDunnageTypes` | `ObservableCollection<Model_SetupTech_DunnageTypeConfig>` |
| [ ] | Add `[ObservableProperty] SelectedTypeFilter` | `Model_SetupTech_DunnageTypeConfig?` |
| [ ] | Add `[ObservableProperty] IsSaving`, `ErrorMessage` | |
| [ ] | Implement `[RelayCommand] LoadAssignmentAsync` | Calls `GetDunnageAssignmentAsync`; calls `GetEnabledDunnageTypesAsync` |
| [ ] | Implement `[RelayCommand] AddDunnageItemAsync` | Calls `UpsertDunnageAssignmentAsync`; adds to `CurrentAssignments` |
| [ ] | Implement `[RelayCommand] RemoveDunnageItemAsync` | Calls `DeleteDunnageAssignmentAsync`; removes from `CurrentAssignments` |
| [ ] | Implement `[RelayCommand] SaveAndContinueAsync` | Raises `NavigateToConfirmationRequested` event |

### `ViewModel_Waitlist_SetupTechConfirmation` (`Feature.Waitlist/ViewModels/SetupTechConfirmation/`)

| Done | Task | Details |
|------|------|---------|
| [ ] | Create `ViewModel_Waitlist_SetupTechConfirmation.cs` | `partial class`, inherits `ObservableObject`, inject `IService_SetupTech` |
| [ ] | Add `[ObservableProperty] ActiveJob` | `Model_SetupTech_ActiveJob?` |
| [ ] | Add `[ObservableProperty] IsSaving`, `SaveSucceeded`, `ErrorMessage` | |
| [ ] | Implement `[RelayCommand] ConfirmSaveAsync` | Calls `IService_SetupTech.SetActiveJobAsync`; on success sets `SaveSucceeded = true`, raises navigation event |
| [ ] | Implement `[RelayCommand] StartOverAsync` | Raises `NavigateToStepOneRequested` event |

---

## Phase 12 — Client Views

### Windows XAML Files (WinUI 3 `Page`)

| Done | Task | File |
|------|------|------|
| [ ] | Create `View_Waitlist_SetupTech.Windows.xaml` | Steps 1 & 2; stepper bar; workcenter `ComboBox`; WO `TextBox` + sequence picker; `{x:Bind ViewModel.Property, Mode=OneWay}` |
| [ ] | Create `View_Waitlist_SetupTechValidation.Windows.xaml` | Two-column card: WO details left, subordinate parts `ListView` right; Approve + Change buttons |
| [ ] | Create `View_Waitlist_SetupTechDunnage.Windows.xaml` | Two-panel: current assignment list (left), dunnage catalog with type tabs (right) |
| [ ] | Create `View_Waitlist_SetupTechConfirmation.Windows.xaml` | Summary card; "Confirm & Save" + "Start Over" buttons |

### Android XAML Files (MAUI `ContentPage`)

| Done | Task | File |
|------|------|------|
| [ ] | Create `View_Waitlist_SetupTech.Android.xaml` | Single-column; full-width barcode input; `Picker` for sequence; `{Binding Property, Mode=OneWay}`; `x:DataType` |
| [ ] | Create `View_Waitlist_SetupTechValidation.Android.xaml` | Scrollable card; `CollectionView` for subordinate parts; min 48 px buttons |
| [ ] | Create `View_Waitlist_SetupTechDunnage.Android.xaml` | Grouped `CollectionView` by type; toggle-style selection; min 48 px tap targets |
| [ ] | Create `View_Waitlist_SetupTechConfirmation.Android.xaml` | Single-column summary; large full-width "Confirm & Save" button at bottom |

### Code-Behind Files (shared — ViewModel binding only)

| Done | Task | File |
|------|------|------|
| [ ] | Create `View_Waitlist_SetupTech.xaml.cs` | Constructor: `InitializeComponent()`, resolve ViewModel via DI, subscribe to navigation events |
| [ ] | Create `View_Waitlist_SetupTechValidation.xaml.cs` | Constructor: same pattern; handle navigation events from ViewModel |
| [ ] | Create `View_Waitlist_SetupTechDunnage.xaml.cs` | Constructor: same pattern |
| [ ] | Create `View_Waitlist_SetupTechConfirmation.xaml.cs` | Constructor: same pattern; on `SaveSucceeded` navigate back to dashboard |

### Code-Behind Validation (for each file)

| Done | Task | Details |
|------|------|---------|
| [ ] | Verify no platform-specific logic in code-behind | Only ViewModel binding + navigation event handlers |
| [ ] | Verify Windows code-behind exposes `public ViewModel_X ViewModel { get; }` | Required for `x:Bind ViewModel.Property` |

---

## Phase 13 — Feature.Waitlist.csproj Updates

| Done | Task | Details |
|------|------|---------|
| [ ] | Add `CommunityToolkit.Mvvm 8.4.2` package reference | `<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />` |
| [ ] | Add `MVVMTK0045` to `<NoWarn>` | `<NoWarn>$(NoWarn);MVVMTK0045</NoWarn>` |
| [ ] | Add platform XAML ItemGroup for `View_Waitlist_SetupTech` | Windows XAML + Android XAML split per `platform-xaml.instructions.md` |
| [ ] | Add platform XAML ItemGroup for `View_Waitlist_SetupTechValidation` | Same pattern |
| [ ] | Add platform XAML ItemGroup for `View_Waitlist_SetupTechDunnage` | Same pattern |
| [ ] | Add platform XAML ItemGroup for `View_Waitlist_SetupTechConfirmation` | Same pattern |

---

## Phase 14 — DI Registration in `MauiProgramExtensions.cs`

| Done | Task | Registration |
|------|------|-------------|
| [ ] | Add `IRepository_InforVisual` | `services.AddSingleton<IRepository_InforVisual, Repository_InforVisual>()` |
| [ ] | Add `IRepository_InforVisualLocal` | `services.AddSingleton<IRepository_InforVisualLocal, Repository_InforVisualLocal>()` |
| [ ] | Add `IService_InforVisual` | `services.AddSingleton<IService_InforVisual, Service_InforVisual>()` |
| [ ] | Add `IRepository_SetupTechActiveJob` | `services.AddSingleton<IRepository_SetupTechActiveJob, Repository_SetupTechActiveJob>()` |
| [ ] | Add `IRepository_SetupTechActiveJobLocal` | `services.AddSingleton<IRepository_SetupTechActiveJobLocal, Repository_SetupTechActiveJobLocal>()` |
| [ ] | Add `IRepository_WorkOrderDunnage` | `services.AddSingleton<IRepository_WorkOrderDunnage, Repository_WorkOrderDunnage>()` |
| [ ] | Add `IRepository_WorkOrderDunnageLocal` | `services.AddSingleton<IRepository_WorkOrderDunnageLocal, Repository_WorkOrderDunnageLocal>()` |
| [ ] | Add `IRepository_SetupTechDunnageTypeConfig` | `services.AddSingleton<IRepository_SetupTechDunnageTypeConfig, Repository_SetupTechDunnageTypeConfig>()` |
| [ ] | Add `IRepository_SetupTechDunnageTypeConfigLocal` | `services.AddSingleton<IRepository_SetupTechDunnageTypeConfigLocal, Repository_SetupTechDunnageTypeConfigLocal>()` |
| [ ] | Add `IService_SetupTech` | `services.AddSingleton<IService_SetupTech, Service_SetupTech>()` |
| [ ] | Add `ViewModel_Waitlist_SetupTech` | `services.AddTransient<ViewModel_Waitlist_SetupTech>()` |
| [ ] | Add `View_Waitlist_SetupTech` | `services.AddTransient<View_Waitlist_SetupTech>()` |
| [ ] | Add `ViewModel_Waitlist_SetupTechValidation` | `services.AddTransient<ViewModel_Waitlist_SetupTechValidation>()` |
| [ ] | Add `View_Waitlist_SetupTechValidation` | `services.AddTransient<View_Waitlist_SetupTechValidation>()` |
| [ ] | Add `ViewModel_Waitlist_SetupTechDunnage` | `services.AddTransient<ViewModel_Waitlist_SetupTechDunnage>()` |
| [ ] | Add `View_Waitlist_SetupTechDunnage` | `services.AddTransient<View_Waitlist_SetupTechDunnage>()` |
| [ ] | Add `ViewModel_Waitlist_SetupTechConfirmation` | `services.AddTransient<ViewModel_Waitlist_SetupTechConfirmation>()` |
| [ ] | Add `View_Waitlist_SetupTechConfirmation` | `services.AddTransient<View_Waitlist_SetupTechConfirmation>()` |

---

## Phase 15 — Build Verification

| Done | Task | Command |
|------|------|---------|
| [ ] | Build Core project — zero errors | `dotnet build MTM_Waitlist_Application/Core/Core/Core.csproj -v minimal` |
| [ ] | Build Data project — zero errors | `dotnet build MTM_Waitlist_Application/Core/Data/Data.csproj -v minimal` |
| [ ] | Build Services project — zero errors | `dotnet build MTM_Waitlist_Application/Core/Services/Services.csproj -v minimal` |
| [ ] | Build Feature.Waitlist — zero errors | `dotnet build MTM_Waitlist_Application/Features/Feature.Waitlist/Feature.Waitlist.csproj -v minimal` |
| [ ] | Build WinUI host — zero errors | `dotnet build MTM_Waitlist_Application/MTM_Waitlist_Application.WinUI/MTM_Waitlist_Application.WinUI.csproj -f net10.0-windows10.0.19041.0 -p:Platform=x64 -v minimal` |
| [ ] | Verify `MVVMTK0045` suppressed in Feature.Waitlist | `dotnet build … 2>&1 \| Select-String "MVVMTK0045"` — should return no matches |
| [ ] | Verify no WMC1006 warnings in WinUI host | `dotnet build … 2>&1 \| Select-String "WMC1006"` — should return no matches |
| [ ] | Build server solution — zero errors | `dotnet build MTM_Waitlist_Server.slnx` |

---

## Phase 16 — Unit Tests

### Service_InforVisual Tests (`Tests/Unit/Services.Tests/InforVisual/`)

| Done | Phase | Task | Subtask |
|------|-------|------|---------|
| [ ] | 16 | Create `Success/Service_InforVisualTests.cs` | Online path: workcenter list returns from cache on second call (no second network call) |
| [ ] | 16 | Test — cache miss triggers online call | First call → network; second call → cache; `InvalidateWorkcenterCache()` → third call → network again |
| [ ] | 16 | Test — online WO header returns success | Connectivity=Online, online repo succeeds → service returns data |
| [ ] | 16 | Create `Failure/Service_InforVisualTests.cs` | Online call fails → workcenter falls back to local cache |
| [ ] | 16 | Test — WO header fails offline | Connectivity=None → `IsSuccess=false`, error message = "not available while offline" |
| [ ] | 16 | Create `Connectivity/Service_InforVisualTests.cs` | Connectivity transitions during workcenter load |

### Service_SetupTech Tests (`Tests/Unit/Services.Tests/SetupTech/`)

| Done | Phase | Task | Subtask |
|------|-------|------|---------|
| [ ] | 16 | Create `Success/Service_SetupTechTests.cs` | `GetActiveJobAsync` online succeeds → returns model |
| [ ] | 16 | Test — dunnage type config syncs to local on online load | |
| [ ] | 16 | Create `Failure/Service_SetupTechTests.cs` | `SetActiveJobAsync` offline → returns failure |
| [ ] | 16 | Test — dunnage upsert offline → returns failure | |
| [ ] | 16 | Create `Connectivity/Service_SetupTechTests.cs` | `GetActiveJobAsync` offline → returns local cached job |

### ViewModel_Waitlist_SetupTech Tests (`Tests/Unit/Feature.Waitlist.Tests/ViewModels/SetupTech/`)

| Done | Phase | Task | Subtask |
|------|-------|------|---------|
| [ ] | 16 | Create `Commands/ViewModel_Waitlist_SetupTechTests.cs` | `LoadWorkcentersAsync` — success path populates `AvailableWorkcenters` |
| [ ] | 16 | Test — `ScanBarcodeAsync` parses `WO-123456:20` → `WorkOrderInput="WO-123456"`, `SequenceInput=20` | |
| [ ] | 16 | Test — `LookupWorkOrderAsync` success → raises `NavigateToValidationRequested` | |
| [ ] | 16 | Test — role guard fires `NavigateBackRequested` for non-SetupTech user | |
| [ ] | 16 | Create `Properties/ViewModel_Waitlist_SetupTechTests.cs` | `WizardStep` increments on `SelectWorkcenterAsync` |

### ViewModel_Waitlist_SetupTechDunnage Tests (`Tests/Unit/Feature.Waitlist.Tests/ViewModels/SetupTechDunnage/`)

| Done | Phase | Task | Subtask |
|------|-------|------|---------|
| [ ] | 16 | Create `Commands/ViewModel_Waitlist_SetupTechDunnageTests.cs` | `LoadAssignmentAsync` pre-populates `CurrentAssignments` when cached assignment exists |
| [ ] | 16 | Test — `AddDunnageItemAsync` calls upsert and adds to `CurrentAssignments` | |
| [ ] | 16 | Test — `RemoveDunnageItemAsync` calls delete and removes from `CurrentAssignments` | |

---

## Architecture Checklist — Before Submitting

| Done | Rule | Check |
|------|------|-------|
| [ ] | No ViewModel references Data project directly | ViewModels only call `IService_*` interfaces |
| [ ] | No Feature.Waitlist references Data project | Feature project only references Core and Services |
| [ ] | All async methods end with `Async` | Scan all new `.cs` files |
| [ ] | All repositories return `Model_Dao_Result<T>` | No `throw` in repository methods |
| [ ] | All ViewModels are `partial` + inherit `ObservableObject` | Verify all 4 ViewModels |
| [ ] | No `new` keyword for services/repos/ViewModels | Constructor injection throughout |
| [ ] | No `Shell.Current.GoToAsync` in Windows code | Windows navigation via raised events + `Frame.Navigate()`/`Frame.GoBack()` |
| [ ] | All compiled bindings on Windows use `x:Bind` (not `{Binding}`) | Scan all Windows XAML files |
| [ ] | All Android XAML files have `x:DataType` on root element | Scan all Android XAML files |
| [ ] | `CommunityToolkit.Mvvm 8.4.2` in `Feature.Waitlist.csproj` | Verify package reference |
| [ ] | `MVVMTK0045` suppressed in `Feature.Waitlist.csproj` | Verify NoWarn |
| [ ] | All new DI registrations: services + repos = `Singleton`; pages + VMs = `Transient` | Verify in `MauiProgramExtensions.cs` |
| [ ] | No `Quantity` column or property in dunnage assignment | FEATURE-07 spec: assignment is a list only, no quantities |
| [ ] | InforVisual queries are READ ONLY — no INSERT/UPDATE/DELETE | Verify server-side DAO |
| [ ] | All datetimes in SQL use `UTC_TIMESTAMP()` — never `NOW()` | Verify all 5 table DDL files |
