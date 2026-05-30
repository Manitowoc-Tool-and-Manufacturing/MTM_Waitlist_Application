# FEATURE07-02: Plan — Setup Technician Module + InforVisual ERP Integration

This is the detailed implementation plan for FEATURE-02 (InforVisual ERP Integration) and FEATURE-07 (Setup Technician Module), implemented together as a single dual-feature release. Reference [FEATURE07-01-Research.md](FEATURE07-01-Research.md) for all code facts, signatures, and naming conventions. Reference [FEATURE-02-InforVisual-ERP-Integration.md](FEATURE-02-InforVisual-ERP-Integration.md) and [FEATURE-07-Setup-Technician-Module.md](FEATURE-07-Setup-Technician-Module.md) for full feature specifications.

No new codebase searches are needed — all facts are in the Research file.

---

## Overview

FEATURE-07 requires every InforVisual data point that FEATURE-02 defines (work orders, sequences, workcenter list, subordinate parts), so both features are built in a single pass. The implementation spans two solutions:

1. **`MTM_Waitlist_Server.slnx`** — server-side API endpoints that proxy InforVisual SQL queries and serve SetupTech data from MySQL.
2. **`MTM_Waitlist_Application.slnx`** — client-side models, repositories, services, ViewModels, and Views.

Database schema changes apply to MySQL `mtm_waitlist_application` and are deployed via migration `V002`.

---

## Architecture Overview

```
User (Setup Tech)
  │
  ▼
View_Waitlist_SetupTech (WinUI 3 Page / MAUI ContentPage)
  │ x:Bind / x:DataType compiled bindings
  ▼
ViewModel_Waitlist_SetupTech  (CommunityToolkit.Mvvm ObservableObject)
  │ constructor injection
  ▼
IService_InforVisual          IService_SetupTech
  │                               │
  ├─ IsOnline?                    ├─ IsOnline?
  │   YES → IRepository_InforVisual    YES → IRepository_SetupTechActiveJob
  │          (calls REST API)               (calls REST API)
  │   NO  → IRepository_InforVisualLocal NO → IRepository_SetupTechActiveJobLocal
  │          (cached workcenters only)      (cached last-known job per workcenter)
  │
  ▼ (server side)
MTM_Waitlist_Server.Admin
  ├─ InforVisualController  →  DAO → VISUAL\MTMFG (SQL Server, ReadOnly)
  └─ SetupTechController    →  usp_SetupTech_* (MySQL)
```

---

## Phase 1 — Database Schema: New Tables, Procedures, Triggers, Indexes, Migration

**Reference:** Research §13, §14, §15, §16, §22

This phase is entirely SQL files. No C# yet. Complete Phase 1 before writing any C# so the stored procedures exist when you implement the server-side repositories.

### 1.1 Create Table DDL Files

Create files in `Database/schema/tables/SetupTech/`:

| File | Notes |
|------|-------|
| `WorkstationActiveJobs.sql` | UNIQUE on `WorkcenterId`, FK to `Users` |
| `WorkstationJobHistory.sql` | FK to `Users`, index on `(WorkcenterId, ActiveFrom)` |
| `WorkOrderDunnageAssignments.sql` | UNIQUE on `(WorkOrderId, SequenceNo, DunnagePartId)`, FK to `Users` |
| `SetupTechDunnageTypeConfig.sql` | UNIQUE on `DunnageTypeId`, no FK (denormalized) |
| `WorkOrderSubordinateParts.sql` | UNIQUE on `(WorkOrderId, SequenceNo, SubPartId)` |

All tables: `Engine=InnoDB`, `DEFAULT CHARSET=utf8mb4`, all datetimes `UTC_TIMESTAMP()`, `CreatedAt` + `UpdatedAt` columns on every table.

### 1.2 Create Index Files

Create files in `Database/indexes/SetupTech/`:

| File | Indexes |
|------|---------|
| `WorkstationActiveJobs_Indexes.sql` | `idx_WorkstationActiveJobs_Workcenter` on `WorkcenterId`; `idx_WorkstationActiveJobs_User` on `SetupTechUserId` |
| `WorkstationJobHistory_Indexes.sql` | `idx_WorkstationJobHistory_Workcenter` on `WorkcenterId`; `idx_WorkstationJobHistory_WorkcenterFrom` on `(WorkcenterId, ActiveFrom)` |
| `WorkOrderDunnageAssignments_Indexes.sql` | `idx_WODunnage_WOSeq` on `(WorkOrderId, SequenceNo)`; `idx_WODunnage_User` on `LastModifiedByUserId` |
| `SetupTechDunnageTypeConfig_Indexes.sql` | `idx_SetupTechDunnageTypeConfig_DisplayOrder` on `DisplayOrder` |
| `WorkOrderSubordinateParts_Indexes.sql` | `idx_WOSubParts_WOSeq` on `(WorkOrderId, SequenceNo)` |

### 1.3 Create Trigger Files

Create files in `Database/triggers/SetupTech/` — one `BEFORE UPDATE` trigger per table:

```sql
-- Example pattern: trg_WorkstationActiveJobs_BeforeUpdate.sql
DROP TRIGGER IF EXISTS trg_WorkstationActiveJobs_BeforeUpdate;
CREATE TRIGGER trg_WorkstationActiveJobs_BeforeUpdate
BEFORE UPDATE ON WorkstationActiveJobs
FOR EACH ROW
BEGIN
    SET NEW.UpdatedAt = UTC_TIMESTAMP();
END;
```

5 trigger files total — one per new table.

### 1.4 Create Stored Procedure Files

Create files in `Database/procedures/SetupTech/` — one file per procedure:

| File | Logic Summary |
|------|--------------|
| `usp_SetupTech_GetActiveJob.sql` | `SELECT * FROM WorkstationActiveJobs WHERE WorkcenterId = @WorkcenterId` |
| `usp_SetupTech_SetActiveJob.sql` | In a transaction: if existing row → INSERT into `WorkstationJobHistory` → DELETE from `WorkstationActiveJobs`; then INSERT new row. Use `DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END`. |
| `usp_SetupTech_GetDunnageAssignment.sql` | `SELECT * FROM WorkOrderDunnageAssignments WHERE WorkOrderId = @WorkOrderId AND SequenceNo = @SequenceNo` |
| `usp_SetupTech_UpsertDunnageAssignment.sql` | `INSERT … ON DUPLICATE KEY UPDATE DunnagePartName = …, LastModifiedByUserId = …` |
| `usp_SetupTech_DeleteDunnageAssignment.sql` | `DELETE FROM WorkOrderDunnageAssignments WHERE Id = @Id` |
| `usp_SetupTech_UpsertSubordinateParts.sql` | Loop upsert or `INSERT … ON DUPLICATE KEY UPDATE` for each part line |
| `usp_SetupTech_GetJobHistory.sql` | `SELECT * FROM WorkstationJobHistory WHERE WorkcenterId = @WorkcenterId ORDER BY ActiveFrom DESC LIMIT @PageSize` |
| `usp_SetupTech_GetEnabledDunnageTypes.sql` | `SELECT * FROM SetupTechDunnageTypeConfig WHERE IsEnabled = 1 ORDER BY DisplayOrder` |
| `usp_SetupTech_UpsertDunnageTypeConfig.sql` | `INSERT … ON DUPLICATE KEY UPDATE DunnageTypeName = …, IsEnabled = …, DisplayOrder = …` |

Each procedure file: `DROP PROCEDURE IF EXISTS …` before `CREATE PROCEDURE`. Every write procedure wraps in `START TRANSACTION … COMMIT` with `DECLARE EXIT HANDLER FOR SQLEXCEPTION BEGIN ROLLBACK; RESIGNAL; END`.

### 1.5 Create Migration

**File:** `Database/migrations/V002__SetupTech_Schema.sql`

The migration file runs all table DDL in dependency order, then creates all indexes, then all triggers, then all stored procedures:

```sql
-- V002__SetupTech_Schema.sql
-- SetupTech module: WorkstationActiveJobs, WorkstationJobHistory,
-- WorkOrderDunnageAssignments, SetupTechDunnageTypeConfig, WorkOrderSubordinateParts
-- All times UTC. Re-runnable (DROP IF EXISTS before each CREATE).

-- Tables
SOURCE Database/schema/tables/SetupTech/WorkstationActiveJobs.sql;
SOURCE Database/schema/tables/SetupTech/WorkstationJobHistory.sql;
SOURCE Database/schema/tables/SetupTech/WorkOrderDunnageAssignments.sql;
SOURCE Database/schema/tables/SetupTech/SetupTechDunnageTypeConfig.sql;
SOURCE Database/schema/tables/SetupTech/WorkOrderSubordinateParts.sql;
-- (Indexes, triggers, procedures follow)
```

### 1.6 Create Seed File

**File:** `Database/seed/03_Seed_SetupTechDunnageTypeConfig.sql`

Insert 13 rows into `SetupTechDunnageTypeConfig`. Types 1–4 and 12–13 enabled (`IsEnabled = 1`), types 5–11 disabled. See Research §22 for the type mapping.

---

## Phase 2 — InforVisual SQL Query Files (FEATURE-02)

**Reference:** Research §6

Create `Database/infor_visual/queries/` folder and add 5 SQL files:

| File | Key SQL Pattern |
|------|----------------|
| `WL_01_GetWorkOrderHeader.sql` | `SELECT … FROM WORK_ORDER wo LEFT JOIN PART p ON wo.PART_ID = p.ID WHERE wo.BASE_ID = @WorkOrderBaseId AND wo.TYPE = 'W'` |
| `WL_02_GetWorkOrderSequences.sql` | `SELECT … FROM OPERATION op LEFT JOIN SHOP_RESOURCE sr ON op.RESOURCE_ID = sr.ID WHERE op.WORKORDER_BASE_ID = @WorkOrderBaseId AND op.WORKORDER_TYPE = 'W' ORDER BY SEQUENCE_NO` |
| `WL_03_GetActiveWorkOrdersForResource.sql` | Filter `op.STATUS IN ('O','R','S')` AND `wo.STATUS IN ('O','R')` |
| `WL_04_GetAllWorkcenters.sql` | `WHERE sr.SCHEDULE_NORMALLY = 'Y'` — **confirm filter with Dan** |
| `WL_05_GetSubordinatePartsForWorkOrder.sql` | `SELECT … FROM WORK_ORDER_MATL m LEFT JOIN PART p ON m.PART_ID = p.ID LEFT JOIN PART_SITE ps ON m.PART_ID = ps.PART_ID AND ps.SITE_ID = '002' WHERE m.ORDER_ID = @WorkOrderBaseId` |

Each file includes a file-level comment: `-- WL_NN_Name.sql / READ ONLY — Infor Visual MTMFG`.

---

## Phase 3 — Server-Side: InforVisual API Endpoints

**Reference:** Research §7

Add to `MTM_Waitlist_Server` solution: an InforVisual module that connects to `VISUAL\MTMFG` (SQL Server, `ApplicationIntent=ReadOnly`) and exposes REST endpoints. This is the server-side half of FEATURE-02.

### 3.1 Server Connection String

Add to `MTM_Waitlist_Server.Admin/appsettings.json` (or `server-settings.json`):
```json
"ConnectionStrings": {
  "InforVisual": "Server=VISUAL;Database=MTMFG;Integrated Security=True;ApplicationIntent=ReadOnly;Connect Timeout=5;Command Timeout=10;"
}
```

### 3.2 Server-Side DAO

**File:** (in server solution Data project) `InforVisual/Dao_InforVisualWorkOrder.cs`

Constructor takes `string connectionString`. Uses `Microsoft.Data.SqlClient`. Enforces `ApplicationIntent=ReadOnly`. Methods:
- `GetWorkOrderHeaderAsync(string workOrderBaseId, CancellationToken ct)`
- `GetWorkOrderSequencesAsync(string workOrderBaseId, CancellationToken ct)`
- `GetActiveWorkOrdersForResourceAsync(string resourceId, CancellationToken ct)`
- `GetAllWorkcentersAsync(CancellationToken ct)`
- `GetSubordinatePartsAsync(string workOrderBaseId, int sequenceNo, CancellationToken ct)`

Each method wraps in try/catch, returns `Model_Dao_Result<T>`. Never throws.

### 3.3 Server-Side API Controller

**File:** `InforVisualController.cs` (in server Admin host or Modules project)

Routes: `[Route("api/infor-visual")]` — maps directly to the endpoints listed in Research §7. Calls the DAO. Returns 200 with data or 503 with error message when Visual is unreachable.

### 3.4 SetupTech API Controller

**File:** `SetupTechController.cs`

Routes: `[Route("api/setup-tech")]` — calls stored procedures via the MySQL data layer. Handles archive-and-replace logic for `SetActiveJob` (delegates to `usp_SetupTech_SetActiveJob`). Also expose `GET /api/dunnage-catalog` which cross-database reads `mtm_receiving_application.dunnage_parts JOIN dunnage_types`.

---

## Phase 4 — Client Core Models: InforVisual (FEATURE-02)

**Reference:** Research §9

Create 4 model files in `Core/Models/InforVisual/`:

- `Model_VisualWorkOrderHeader.cs` — properties: `WorkOrderId`, `PartId`, `PartDescription`, `WorkOrderStatus`, `DesiredQty`, `WantDate`, `SiteId`
- `Model_VisualWorkOrderSequence.cs` — properties: `WorkOrderId`, `SequenceNo`, `WorkcenterId`, `WorkcenterDescription`, `SequenceStatus`, `SetupCompleted`, `CompletedQty`, `TargetQty`, `SchedStart`, `SchedFinish`
- `Model_VisualWorkcenter.cs` — properties: `WorkcenterId`, `Description`, `ResourceType`, `DepartmentId`, `ScheduleGroup`
- `Model_Visual_SubordinatePart.cs` — properties: `WorkOrderId`, `SequenceNo`, `PartId`, `PartDescription`, `RequiredQty`, `QtyOnHand`

All models: `public sealed record` or `public sealed class` with only get-able properties. `/// <summary>` on class and all properties. No behavior.

---

## Phase 5 — Client Core Models: SetupTech (FEATURE-07)

**Reference:** Research §10

Create 5 model files in `Core/Models/SetupTech/`:

- `Model_SetupTech_ActiveJob.cs`
- `Model_SetupTech_JobHistoryEntry.cs`
- `Model_SetupTech_DunnageAssignment.cs` — **`Quantity` property must NOT exist** per spec
- `Model_SetupTech_SubordinatePart.cs`
- `Model_SetupTech_DunnageTypeConfig.cs`

---

## Phase 6 — Client Core Constants

**Reference:** Research §8

Add all new endpoint constants to `Core/Constants/Api/Constants_Api.cs`. Group under comments:
```csharp
// ── InforVisual ──
// ── SetupTech ──
```

---

## Phase 7 — Client Core Interfaces (FEATURE-02 + FEATURE-07)

**Reference:** Research §11, §12

Create all interfaces. All method signatures use `CancellationToken ct = default` as the last parameter. All return `Task<Model_Dao_Result<T>>`.

### InforVisual interfaces (`Core/Interfaces/InforVisual/`)

1. `IService_InforVisual.cs` — 5 query methods + `InvalidateWorkcenterCache()` (see Research §11 for full signature)
2. `IRepository_InforVisual.cs` — same 5 query methods; no caching, direct REST calls
3. `IRepository_InforVisualLocal.cs` — `GetCachedWorkcentersAsync()` + `SaveWorkcentersAsync()` only (sqlite-net-pcl)

### SetupTech interfaces (`Core/Interfaces/SetupTech/`)

4. `IService_SetupTech.cs` — all user-facing operations
5. `IRepository_SetupTechActiveJob.cs` — `GetActiveJobAsync(string workcenterId)`, `SetActiveJobAsync(Model_SetupTech_ActiveJob job)`
6. `IRepository_SetupTechActiveJobLocal.cs` — same signatures, operates on local cache
7. `IRepository_WorkOrderDunnage.cs` — Get/Upsert/Delete dunnage assignments
8. `IRepository_WorkOrderDunnageLocal.cs` — local cache variants
9. `IRepository_SetupTechDunnageTypeConfig.cs` — `GetEnabledDunnageTypesAsync()`
10. `IRepository_SetupTechDunnageTypeConfigLocal.cs` — `GetCachedDunnageTypesAsync()` + `SaveDunnageTypesAsync()`

---

## Phase 8 — Client Data Repositories (FEATURE-02)

**Reference:** Research §5 (patterns)

### 8.1 `Repository_InforVisual` (online, calls REST API)

**File:** `Data/Repositories/InforVisual/Repository_InforVisual.cs`

Implements `IRepository_InforVisual`. Constructor takes `IApiClient`. All 5 methods delegate to `_apiClient.GetAsync<T>(string.Format(Constants_Api.VisualXxx, args), ct)`. Never throws — `IApiClient` returns `Model_Dao_Result`.

### 8.2 `Repository_InforVisualLocal` (offline, workcenter cache only)

**File:** `Data/Repositories/InforVisual/Repository_InforVisualLocal.cs`

Implements `IRepository_InforVisualLocal`. Constructor takes `LocalDbContext`. Uses `sqlite-net-pcl` with an `Entity_VisualWorkcenter` table (add `[Table("VisualWorkcenterCache")]` entity).

---

## Phase 9 — Client Data Repositories (FEATURE-07)

### 9.1 Online repositories (call REST API via `IApiClient`)

- `Repository_SetupTechActiveJob.cs` — GET and POST to `/api/setup-tech/active-job/…`
- `Repository_WorkOrderDunnage.cs` — GET/PUT/DELETE to `/api/setup-tech/dunnage-assignment/…`
- `Repository_SetupTechDunnageTypeConfig.cs` — GET from `/api/setup-tech/dunnage-types`

All 3 files in `Data/Repositories/SetupTech/`.

### 9.2 Local repositories (sqlite-net-pcl via `LocalDbContext`)

- `Repository_SetupTechActiveJobLocal.cs`
- `Repository_WorkOrderDunnageLocal.cs`
- `Repository_SetupTechDunnageTypeConfigLocal.cs`

Each wraps all operations in try/catch. Returns `Model_Dao_Result.Failure(...)` on exception. Uses `await _context.InitializeAsync()` before every query.

---

## Phase 10 — Client Services (FEATURE-02 + FEATURE-07)

### 10.1 `Service_InforVisual`

**File:** `Services/InforVisual/Service_InforVisual.cs`

- Connectivity-aware: online calls go to `Repository_InforVisual`; offline fallback for workcenters goes to `Repository_InforVisualLocal`.
- **Workcenter cache:** In-memory `List<Model_VisualWorkcenter>` field. If not null and not invalidated, return cached list without a network call. Populate from online repository on first call; save to local repo for offline use. `InvalidateWorkcenterCache()` sets field to null.
- Work order queries (header, sequences, subordinate parts, active WOs): always require network. If offline, return `Model_Dao_Result.Failure("Infor Visual is not available while offline.")`.

### 10.2 `Service_SetupTech`

**File:** `Services/SetupTech/Service_SetupTech.cs`

- Connectivity-aware: routes to online or local repositories based on `IConnectivity.NetworkAccess == NetworkAccess.Internet`.
- Active job: online → `Repository_SetupTechActiveJob`; offline → `Repository_SetupTechActiveJobLocal` (shows last-known job, read-only while offline).
- Dunnage: online → `Repository_WorkOrderDunnage`; offline → `Repository_WorkOrderDunnageLocal` (can view cached assignments; write operations queue or reject while offline — show clear error message).
- Dunnage type config: online → `Repository_SetupTechDunnageTypeConfig`; offline → `Repository_SetupTechDunnageTypeConfigLocal`.
- `SetActiveJobAsync`: validates role, calls online repo (cannot set active job offline — requires network).

---

## Phase 11 — Client ViewModels (FEATURE-07)

**Reference:** Research §18

All 4 ViewModels: `partial class`, inherit `ObservableObject`, `CommunityToolkit.Mvvm 8.4.2`. Namespace: `Feature.Waitlist.ViewModels`.

### 11.1 `ViewModel_Waitlist_SetupTech`

**File:** `Feature.Waitlist/ViewModels/SetupTech/ViewModel_Waitlist_SetupTech.cs`

Constructor injection: `IService_InforVisual`, `IService_SetupTech`, `IService_Auth`.

Role guard in constructor (or `OnNavigatedToAsync` if navigation lifecycle is wired):
```csharp
if (!HasRequiredRole()) { RaiseNavigateBackRequested(); return; }
```

Wizard state: `WizardStep` integer (1 = workcenter select, 2 = scan/enter WO, ready to navigate to validation screen). Steps 3–5 are in separate ViewModels / pages.

`ScanBarcodeAsync` command: parses a barcode string of the form `{WorkOrderId}:{SequenceNo}` (or platform barcode scanner input); populates `WorkOrderInput` and `SequenceInput`.

`LookupWorkOrderAsync` command: calls `IService_InforVisual.GetWorkOrderHeaderAsync` and `GetWorkOrderSequencesAsync`. On success, navigates to validation screen passing `WorkOrderHeader` and `SubordinateParts`.

### 11.2 `ViewModel_Waitlist_SetupTechValidation`

**File:** `Feature.Waitlist/ViewModels/SetupTechValidation/ViewModel_Waitlist_SetupTechValidation.cs`

Receives `WorkOrderHeader`, `SubordinateParts`, `SelectedSequence` via navigation parameter (or ctor injection through a shared state service).

`ApproveAsync`: navigates to dunnage screen.
`ChangeWorkOrderAsync`: raises event to navigate back to Step 2.

### 11.3 `ViewModel_Waitlist_SetupTechDunnage`

**File:** `Feature.Waitlist/ViewModels/SetupTechDunnage/ViewModel_Waitlist_SetupTechDunnage.cs`

On load: calls `IService_SetupTech.GetDunnageAssignmentAsync(workOrderId, sequenceNo)` to pre-populate.
If no cached assignment, loads dunnage catalog via `IService_SetupTech.GetDunageCatalogAsync()`.
Calls `IService_SetupTech.GetEnabledDunnageTypesAsync()` to build category tabs.

`SelectedTypeFilter`: filters the dunnage catalog to the selected dunnage type tab.
`AddDunnageItemAsync(item)`: calls upsert repo.
`RemoveDunnageItemAsync(item)`: calls delete repo.
`SaveAndContinueAsync`: navigates to confirmation screen.

### 11.4 `ViewModel_Waitlist_SetupTechConfirmation`

**File:** `Feature.Waitlist/ViewModels/SetupTechConfirmation/ViewModel_Waitlist_SetupTechConfirmation.cs`

`ConfirmSaveAsync`: calls `IService_SetupTech.SetActiveJobAsync(activeJob)`. On success, sets `SaveSucceeded = true` + navigates back to dashboard (or shows success state).
`StartOverAsync`: navigates back to Step 1 (workcenter select).

---

## Phase 12 — Client Views (FEATURE-07)

**Reference:** Research §19; Feature spec mockup → view mapping

4 screens × 2 platforms = **8 XAML files** + **4 code-behind files**.

### General rules for all Windows XAML

- Root element: `<Page>` (not `<ContentPage>`)
- Namespace: `xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"`
- Bindings: `{x:Bind ViewModel.Property, Mode=OneWay}` (or `TwoWay` for inputs)
- Code-behind: expose `public ViewModel_Waitlist_X ViewModel { get; }` property, set in constructor after `InitializeComponent()`
- No MAUI controls: use `StackPanel`, `Grid`, `Border`, `Button`, `TextBlock`, `ComboBox`, `ListView`, `ItemsRepeater`, `ScrollViewer`, `AutoSuggestBox`, `ProgressRing` from `Microsoft.UI.Xaml.Controls`

### General rules for all Android XAML

- Root element: `<ContentPage>` with `x:DataType="vm:ViewModel_Waitlist_X"`
- Bindings: `{Binding Property, Mode=OneWay}` (or `TwoWay` for inputs)
- Minimum tap target: 48 px height on all interactive elements
- Use `CollectionView`, `StackLayout`, `Label`, `Entry`, `Button`, `ActivityIndicator` from MAUI

### 12.1 `View_Waitlist_SetupTech` (Steps 1 & 2)

**Windows layout:**
- Stepper bar at top showing steps 1–5 with current step highlighted
- Step 1: Two-column layout — workcenter list/dropdown (left), workcenter details (right); `ComboBox` with search for workcenter selection
- Step 2: Work order input section; large `TextBox` for barcode/manual entry with `AutoSuggestBox` for sequence selection; `Button` "Look Up Work Order"

**Android layout:**
- Single-column scroll; large full-width input for barcode scanning; `Picker` for sequence after WO entered

### 12.2 `View_Waitlist_SetupTechValidation` (Step 3)

**Windows layout:**
- Two-column card: left column shows Work Order details (part number, type, status, desired qty); right column shows subordinate parts as a `ListView` (part ID, description, required qty, qty on hand)
- Footer buttons: "Change Work Order / Sequence" (back) + "Approve →" (continue)

**Android layout:**
- Scrollable single-column card with section headers; subordinate parts as a `CollectionView`; action buttons at bottom

### 12.3 `View_Waitlist_SetupTechDunnage` (Step 4)

**Windows layout:**
- Two-panel: left panel = current assignment list (can remove items); right panel = dunnage catalog picker with category tabs at top (`TabView` or custom `ListView` for type tabs), catalog items below
- Search/filter within catalog items

**Android layout:**
- Accordion-style `CollectionView` grouped by dunnage type; tapping an item adds/removes it from assignment; "Selected" badge on chosen items

### 12.4 `View_Waitlist_SetupTechConfirmation` (Step 5)

**Windows layout:**
- Summary card showing: workcenter, work order + sequence, part number, count of dunnage items assigned; "Confirm & Save" primary button + "Start Over" secondary

**Android layout:**
- Single-column summary; large "Confirm & Save" button at bottom (min 48 px)

---

## Phase 13 — Feature.Waitlist.csproj Updates

**Reference:** Research §17; `platform-xaml.instructions.md`

1. Add `CommunityToolkit.Mvvm` package reference:
   ```xml
   <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />
   ```

2. Add `<NoWarn>$(NoWarn);MVVMTK0045</NoWarn>` to the property group.

3. Add platform XAML ItemGroups for each of the 4 screens. Each screen needs:
   - `EmbeddedResource` with `LogicalName` for the Windows XAML (compiled by WinUI 3 build)
   - `MauiXaml` for the Android XAML
   - Follow the exact pattern from `platform-xaml.instructions.md`

---

## Phase 14 — DI Registration in `MauiProgramExtensions.cs`

**Reference:** Research §20

Add all new InforVisual and SetupTech registrations to `AddSharedServices()` in the correct section order. Full list of registrations is in Research §20.

---

## Phase 15 — Build Verification

Run builds in order to catch layer violations early:

```powershell
# 1. Core only
dotnet build MTM_Waitlist_Application/Core/Core/Core.csproj -v minimal

# 2. Data only
dotnet build MTM_Waitlist_Application/Core/Data/Data.csproj -v minimal

# 3. Services only
dotnet build MTM_Waitlist_Application/Core/Services/Services.csproj -v minimal

# 4. Feature.Waitlist only
dotnet build MTM_Waitlist_Application/Features/Feature.Waitlist/Feature.Waitlist.csproj -v minimal

# 5. Full solution (Windows TFM only for fast iteration)
dotnet build MTM_Waitlist_Application/MTM_Waitlist_Application.WinUI/MTM_Waitlist_Application.WinUI.csproj `
    -f net10.0-windows10.0.19041.0 -p:Platform=x64 -v minimal
```

---

## Phase 16 — Unit Tests

**Reference:** `testing.instructions.md`

Test projects and paths follow the mirror-path + category convention:

| Source file | Test file | Category |
|-------------|-----------|----------|
| `Services/InforVisual/Service_InforVisual.cs` | `Tests/Unit/Services.Tests/InforVisual/Success/Service_InforVisualTests.cs` | Success |
| `Services/InforVisual/Service_InforVisual.cs` | `Tests/Unit/Services.Tests/InforVisual/Failure/Service_InforVisualTests.cs` | Failure |
| `Services/InforVisual/Service_InforVisual.cs` | `Tests/Unit/Services.Tests/InforVisual/Connectivity/Service_InforVisualTests.cs` | Connectivity |
| `Services/SetupTech/Service_SetupTech.cs` | `Tests/Unit/Services.Tests/SetupTech/Success/Service_SetupTechTests.cs` | Success |
| `Services/SetupTech/Service_SetupTech.cs` | `Tests/Unit/Services.Tests/SetupTech/Failure/Service_SetupTechTests.cs` | Failure |
| `Services/SetupTech/Service_SetupTech.cs` | `Tests/Unit/Services.Tests/SetupTech/Connectivity/Service_SetupTechTests.cs` | Connectivity |
| `Feature.Waitlist/.../ViewModel_Waitlist_SetupTech.cs` | `Tests/Unit/Feature.Waitlist.Tests/ViewModels/SetupTech/Commands/ViewModel_Waitlist_SetupTechTests.cs` | Commands |
| `Feature.Waitlist/.../ViewModel_Waitlist_SetupTech.cs` | `Tests/Unit/Feature.Waitlist.Tests/ViewModels/SetupTech/Properties/ViewModel_Waitlist_SetupTechTests.cs` | Properties |

Key test scenarios:
- `Service_InforVisual`: workcenter cache hit (no second network call), cache miss triggers online call, online failure falls back to local cache
- `Service_InforVisual`: Visual offline → correct `Failure` result for WO queries (no local fallback for WO data)
- `Service_SetupTech`: `SetActiveJobAsync` offline → returns failure
- `Service_SetupTech`: `GetActiveJobAsync` offline → returns local cached job
- `ViewModel_Waitlist_SetupTech`: role guard redirects non-SetupTech user
- `ViewModel_Waitlist_SetupTech`: `ScanBarcodeAsync` parses WO + Seq from barcode string
- `ViewModel_Waitlist_SetupTechDunnage`: pre-populates existing assignment on load

---

## Implementation Order Summary

```
Phase 1  ── Database schema (SQL files, migration)
    ↓
Phase 2  ── InforVisual SQL query files
    ↓
Phase 3  ── Server-side API endpoints (MTM_Waitlist_Server)
    ↓
Phase 4  ── Client Core Models: InforVisual
    ↓
Phase 5  ── Client Core Models: SetupTech
    ↓
Phase 6  ── Client Core Constants (Constants_Api entries)
    ↓
Phase 7  ── Client Core Interfaces
    ↓
Phase 8  ── Client Data Repositories: InforVisual
    ↓
Phase 9  ── Client Data Repositories: SetupTech
    ↓
Phase 10 ── Client Services (Service_InforVisual + Service_SetupTech)
    ↓
Phase 11 ── Client ViewModels (4 VMs)
    ↓
Phase 12 ── Client Views (8 XAML + 4 code-behinds)
    ↓
Phase 13 ── Feature.Waitlist.csproj updates
    ↓
Phase 14 ── DI registration in MauiProgramExtensions.cs
    ↓
Phase 15 ── Build verification
    ↓
Phase 16 ── Unit tests
```

> **Dependency note:** Phases 4–10 are independent of the server (Phase 3) once the interface contracts are defined. Client development can proceed in parallel with server development as long as the API endpoint signatures match Research §7.
