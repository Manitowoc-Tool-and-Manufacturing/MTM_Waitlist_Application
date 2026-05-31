# FEATURE-07: Setup Technician Module

**Status:** Ready to Design  
**Priority:** Medium — Required for Full MVP  
**Depends On:** FEATURE-01 (auth), FEATURE-02 (Infor Visual), FEATURE-04 (live queue)  
**Blocks:** Nothing  

---

## Overview

The Setup Technician Module allows setup techs to register the current job running at a workstation. They scan a work order barcode, the app retrieves job details from Infor Visual, they confirm supplemental material requirements (subordinate parts, dunnage, coils, flatstock), and the app saves the active job configuration for that workstation. Prior job configurations are archived to a history table for analytics.

**Authorized roles:** `SetupTech`, `Admin`, `Developer`

This feature is Windows-primary (floor kiosk) but must also function on Android companion devices.

---

## IMPORTANT

Upon the completion of this Feature the AI Agent is responsible for generating or updating any documentation files inside of .github/instructions, .github/copilot-instructions.md, AGENTS.MD, README.md (the core Readme file), as well as the creation of an md file we can later use when we get to the User Guide Phase of development.

---

## User Workflow

### Step 1 — Select Workstation

The setup tech selects (or confirms) the workstation the job will be running on.

- Workstation list is pulled from `Service_InforVisual.GetAllWorkcentersAsync()` (same cached source as FEATURE-03)
- Windows: dropdown with search
- Android: scrollable large-tap-target list

### Step 2 — Scan Work Order Barcode

The setup tech scans the work order barcode from the physical job traveler (printed separately — not issued by this application). The barcode encodes:
- **Work Order number** (e.g., `WO-123456`)
- **Sequence number** (e.g., `20`) — what the team calls the "operation" or step on the work order; always use the label **"Sequence"** or **"Seq"** in all UI

The app accepts manual entry as a fallback if a scanner is unavailable.

### Step 3 — Infor Visual Data Retrieval

After receiving the Work Order + Sequence pair, the app calls the REST API, which queries Infor Visual (`VISUAL\MTMFG`, read-only) and returns:

| Field | Source |
|---|---|
| Part Number | `WORK_ORDER.PART_ID` |
| Part Type | `PART.PART_TYPE` — Finished Goods, WIP, Outside Service |
| Subordinate Parts | `WORK_ORDER_MATL` rows linked to the Work Order |

> All Infor Visual queries execute on the server via `IApiClient` — the client app never connects to SQL Server directly.

**Infor Visual terminology reminder:** The individual steps on a work order are in the `OPERATION` table, keyed by `SEQUENCE_NO`. Always label these as "Sequence" in the UI.

### Step 4 — Validation Screen

A confirmation screen shows the retrieved job data. The setup tech can either:
- **Approve** — proceed to the dunnage selection step
- **Change Work Order / Sequence** — return to Step 2; the sequence selector is a `ComboBox` populated from the sequences available on the entered work order (Infor Visual lookup)

### Step 5 — Dunnage Selection

Dunnage is not stored in Infor Visual. The MySQL `mtm_waitlist_application` database caches the dunnage assignment for each `WorkOrder + Sequence` combination to avoid repeating this step on every run.

> **No quantities.** The setup tech records *which* dunnage items belong to the job — not how many. Quantities are a physical/production concern tracked elsewhere. The assignment is simply a list of dunnage items associated with the WO/Sequence pair.

**Selection behavior:**
- If a saved dunnage assignment already exists for this WO/Sequence → pre-populate the selection; the setup tech can accept as-is or modify
- If no assignment exists → the setup tech selects from the dunnage catalog (sourced from `mtm_receiving_application.dunnage_parts` via cross-database API read)

**Setup techs can always modify** the dunnage assignment regardless of whether a cached assignment exists. Common reasons: dunnage added incorrectly, new container type assigned to the job.

**Dunnage types supported:**
- Component parts (from `WORK_ORDER_MATL` — already retrieved in Step 3)
- Dunnage containers (skids, Gaylords, boxes — from `mtm_receiving_application`)
- Coils / flat stock (material type)

### Step 6 — Save Active Job Configuration

Upon completing the dunnage section the system:

1. Checks for an existing active job record for the selected workstation in `WorkstationActiveJobs`
2. If one exists → moves it to `WorkstationJobHistory` (for analytics — FEATURE-06)
3. Saves the new configuration to `WorkstationActiveJobs`

The active job record captures:
- Workstation ID
- Work Order + Sequence
- Part Number and Part Type
- Subordinate Parts (serialized or normalized)
- Dunnage assignments
- Setup Tech user ID
- Timestamps

---

## Infor Visual Query Reference

The following existing queries from the MTM Receiving Application illustrate the patterns to follow. The API server replicates this query approach for work order lookups:

| File | Purpose |
|---|---|
| `01_GetPOWithParts.sql` | Pattern for header + line item retrieval (mirror for WO + WORK_ORDER_MATL) |
| `03_GetPartByNumber.sql` | Part lookup by `PART_ID` |
| `CustomerPullPack/01_GetCustomerPullPackDemand.sql` | Subordinate/component part demand pattern (mirrors subordinate WO material lookup) |

**Key Infor Visual tables for this feature:**

| Table | Used For |
|---|---|
| `WORK_ORDER` | Work order header — `ORDER_ID`, `PART_ID`, `STATUS` |
| `WORK_ORDER_MATL` | Subordinate/component material lines — `ORDER_ID`, `PART_ID`, `REQUIRED_QTY` |
| `OPERATION` | Work order sequences — `ORDER_ID`, `SEQUENCE_NO`, `WORK_CENTER_ID` |
| `PART` | Part type and description |
| `PART_SITE` | Per-site inventory — `PART_ID`, `SITE_ID`, `QTY_ON_HAND`; JOIN to `WORK_ORDER_MATL.PART_ID` = `PART_SITE.PART_ID` WHERE `SITE_ID = '002'` to get on-hand quantity |
| `SHOP_RESOURCE` | Workcenter list |

> Reference CSV schema files at `Documents/InforVisualRelated/CSV_Documents/` for full column details.

---

## Dunnage Catalog Integration

Dunnage master data lives in the `mtm_receiving_application` MySQL database. The REST API exposes a read-only endpoint for fetching dunnage parts. The relevant source tables in that database are:

### `mtm_receiving_application` — Source Tables

#### `dunnage_types`
| Column | Type | Notes |
|---|---|---|
| `id` | INT AUTO_INCREMENT PK | |
| `type_name` | VARCHAR(100) UNIQUE | Display name — e.g., "Pallets / Skids", "Gaylords / Bulk Bins" |
| `icon` | VARCHAR(50) | MaterialIconKind name used in the receiving app UI |
| `image_path` | VARCHAR(255) NULL | Optional image for the type |
| `created_by` | VARCHAR(50) | |
| `created_date` | DATETIME | |
| `modified_by` | VARCHAR(50) NULL | |
| `modified_date` | DATETIME NULL | |

**Seeded types (from `03_seed_dunnage_data.sql`):**
1. Pallets / Skids
2. Cardboard Sheets / Slip Sheets
3. Corrugated Boxes
4. Gaylords / Bulk Bins
5. Stretch Film / Shrink Wrap
6. Bags
7. Tape / Strapping / Banding
8. Edge Protectors
9. Foam / Molded Inserts
10. Returnable Racks - John Deere
11. Returnable Racks - Other
12. Returnable Totes
13. Returnable Baskets / Wire Containers

#### `dunnage_parts`
| Column | Type | Notes |
|---|---|---|
| `id` | INT AUTO_INCREMENT PK | |
| `part_id` | VARCHAR(50) UNIQUE | Business identifier / SKU (e.g., "40x48", "42x42") |
| `type_id` | INT FK → `dunnage_types.id` | |
| `spec_values` | JSON NULL | Dynamic attributes (dimensions, capacity, material, etc.) |
| `image_path` | VARCHAR(255) NULL | |
| `quantity_type` | VARCHAR(100) DEFAULT 'Quantity' | Label header used on printed dunnage labels |
| `home_location` | VARCHAR(100) NULL | Default storage location |
| `created_by` | VARCHAR(50) | |
| `created_date` | DATETIME | |
| `modified_by` | VARCHAR(50) NULL | |
| `modified_date` | DATETIME NULL | |

#### `dunnage_specs`
| Column | Type | Notes |
|---|---|---|
| `id` | INT AUTO_INCREMENT PK | |
| `type_id` | INT NOT NULL FK → `dunnage_types.id` ON DELETE CASCADE | |
| `spec_key` | VARCHAR(100) NOT NULL | Attribute name (length, width, material, etc.) |
| `spec_value` | JSON NULL | Flexible value for the spec |
| `created_by` | VARCHAR(50) NOT NULL | |
| `created_date` | DATETIME NOT NULL | |
| `modified_by` | VARCHAR(50) NULL | |
| `modified_date` | DATETIME NULL | |

**Unique constraint:** `UK_dunnage_specs_type_key (type_id, spec_key)`

### UI Type Filtering — `SetupTechDunnageTypeConfig`

Not all 13 dunnage types in `mtm_receiving_application` are relevant to the Setup Tech dunnage picker. For example, stretch film and tape are consumables that do not need to be assigned as outgoing dunnage for a WO/Sequence. The `SetupTechDunnageTypeConfig` table in `mtm_waitlist_application` controls which types are shown as category tabs in the Step 4 dunnage picker UI.

An admin (via a future admin screen) can enable or disable individual types and set their display order. The `DunnageTypeId` and `DunnageTypeName` columns mirror the receiving app's `dunnage_types.id` and `dunnage_types.type_name` — they are denormalized so the UI can function offline.

The `mtm_waitlist_application` database stores only the **assignment** of dunnage to a WO/Sequence pair and the UI filter config — it does not duplicate the full catalog.

---

## New Database Tables Needed (`mtm_waitlist_application`)

```sql
-- Stores the current active job configuration per workstation
CREATE TABLE WorkstationActiveJobs (
    Id                  INT UNSIGNED NOT NULL AUTO_INCREMENT,
    WorkcenterId        VARCHAR(100) NOT NULL,       -- SHOP_RESOURCE.ID from Infor Visual
    WorkOrderId         VARCHAR(50)  NOT NULL,
    SequenceNo          INT          NOT NULL,
    PartId              VARCHAR(50)  NOT NULL,
    PartType            VARCHAR(50)  NOT NULL,       -- FinishedGoods, WIP, OutsideService
    SetupTechUserId     INT UNSIGNED NOT NULL,
    ActiveSince         DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
    CreatedAt           DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
    UpdatedAt           DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
    PRIMARY KEY pk_WorkstationActiveJobs (Id),
    UNIQUE uq_WorkstationActiveJobs_Workcenter (WorkcenterId),
    CONSTRAINT fk_WorkstationActiveJobs_User FOREIGN KEY (SetupTechUserId) REFERENCES Users (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- History of all prior job configurations per workstation (for analytics)
CREATE TABLE WorkstationJobHistory (
    Id                  INT UNSIGNED NOT NULL AUTO_INCREMENT,
    WorkcenterId        VARCHAR(100) NOT NULL,
    WorkOrderId         VARCHAR(50)  NOT NULL,
    SequenceNo          INT          NOT NULL,
    PartId              VARCHAR(50)  NOT NULL,
    PartType            VARCHAR(50)  NOT NULL,
    SetupTechUserId     INT UNSIGNED NOT NULL,
    ActiveFrom          DATETIME     NOT NULL,
    ActiveUntil         DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
    CreatedAt           DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
    UpdatedAt           DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
    PRIMARY KEY pk_WorkstationJobHistory (Id),
    CONSTRAINT fk_WorkstationJobHistory_User FOREIGN KEY (SetupTechUserId) REFERENCES Users (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Caches dunnage assignments per WO/Sequence combination
-- No quantity column — the assignment records WHICH dunnage items belong to the job, not how many
CREATE TABLE WorkOrderDunnageAssignments (
    Id              INT UNSIGNED NOT NULL AUTO_INCREMENT,
    WorkOrderId     VARCHAR(50)  NOT NULL,
    SequenceNo      INT          NOT NULL,
    DunnagePartId   INT NOT NULL,               -- references mtm_receiving_application.dunnage_parts.id (INT, not UNSIGNED)
    DunnagePartName VARCHAR(200) NOT NULL,      -- denormalized for offline display
    DunnageTypeId   INT NOT NULL,               -- references mtm_receiving_application.dunnage_types.id (INT, not UNSIGNED)
    DunnageTypeName VARCHAR(100) NOT NULL,      -- denormalized
    LastModifiedByUserId INT UNSIGNED NOT NULL,
    CreatedAt       DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
    UpdatedAt       DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
    PRIMARY KEY pk_WorkOrderDunnageAssignments (Id),
    UNIQUE uq_WorkOrderDunnageAssignments_WO_Seq_Part (WorkOrderId, SequenceNo, DunnagePartId),
    CONSTRAINT fk_WorkOrderDunnageAssignments_User FOREIGN KEY (LastModifiedByUserId) REFERENCES Users (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Controls which dunnage types from mtm_receiving_application.dunnage_types
-- appear as category tabs in the Step 4 dunnage picker UI.
-- DunnageTypeId / DunnageTypeName mirror receiving app values and are denormalized for offline use.
-- Default seed: types 1-4 and 12-13 enabled (Pallets, Cardboard, Corrugated, Gaylords,
--   Returnable Totes, Returnable Baskets); all others disabled by default.
CREATE TABLE SetupTechDunnageTypeConfig (
    Id              INT UNSIGNED NOT NULL AUTO_INCREMENT,
    DunnageTypeId   INT NOT NULL,                   -- mirrors mtm_receiving_application.dunnage_types.id (INT, not UNSIGNED)
    DunnageTypeName VARCHAR(100) NOT NULL,          -- denormalized; refreshed via API sync
    IsEnabled       TINYINT(1)   NOT NULL DEFAULT 1,  -- 1 = shown in UI, 0 = hidden
    DisplayOrder    INT UNSIGNED NOT NULL DEFAULT 99, -- sort order of category tabs in Step 4
    CreatedAt       DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
    UpdatedAt       DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
    PRIMARY KEY pk_SetupTechDunnageTypeConfig (Id),
    UNIQUE uq_SetupTechDunnageTypeConfig_TypeId (DunnageTypeId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Stores subordinate (component) parts per WO/Sequence, cached from Infor Visual
CREATE TABLE WorkOrderSubordinateParts (
    Id              INT UNSIGNED NOT NULL AUTO_INCREMENT,
    WorkOrderId     VARCHAR(50)  NOT NULL,
    SequenceNo      INT          NOT NULL,
    SubPartId       VARCHAR(50)  NOT NULL,       -- PART_ID from Infor Visual
    SubPartDesc     VARCHAR(200) NULL,
    RequiredQty     DECIMAL(10,4) NOT NULL DEFAULT 1,
    QtyOnHand       DECIMAL(10,4) NOT NULL DEFAULT 0,   -- PART_SITE.QTY_ON_HAND at cache time (Site 002)
    CachedAt        DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
    CreatedAt       DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
    UpdatedAt       DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
    PRIMARY KEY pk_WorkOrderSubordinateParts (Id),
    UNIQUE uq_WorkOrderSubordinateParts_WO_Seq_Part (WorkOrderId, SequenceNo, SubPartId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## New Stored Procedures Needed

| Procedure | Purpose |
|---|---|
| `usp_SetupTech_GetActiveJob` | Get current active job for a workstation |
| `usp_SetupTech_SetActiveJob` | Archive current job to history, save new active job |
| `usp_SetupTech_GetDunnageAssignment` | Get cached dunnage for a WO/Sequence pair |
| `usp_SetupTech_UpsertDunnageAssignment` | Insert or update a dunnage assignment line |
| `usp_SetupTech_DeleteDunnageAssignment` | Remove a dunnage line from an assignment |
| `usp_SetupTech_UpsertSubordinateParts` | Cache subordinate parts returned from Visual |
| `usp_SetupTech_GetJobHistory` | Retrieve job history for a workstation (analytics) |
| `usp_SetupTech_GetEnabledDunnageTypes` | Get `SetupTechDunnageTypeConfig` rows where `IsEnabled = 1`, ordered by `DisplayOrder` — used to populate the category tabs in Step 4 |
| `usp_SetupTech_UpsertDunnageTypeConfig` | Insert or update a `SetupTechDunnageTypeConfig` row (admin use; refreshes denormalized name from API sync) |

---

## New Core Models Needed

```
Core/Models/SetupTech/
  Model_SetupTech_ActiveJob.cs
      ← WorkcenterId, WorkOrderId, SequenceNo, PartId, PartType,
         SubordinateParts, DunnageAssignments, SetupTechUserId, ActiveSince

  Model_SetupTech_JobHistoryEntry.cs
      ← same fields + ActiveFrom, ActiveUntil

  Model_SetupTech_DunnageAssignment.cs
      ← AssignmentId, WorkOrderId, SequenceNo, DunnagePartId, DunnagePartName,
         DunnageTypeId, DunnageTypeName, LastModifiedByUserId
         // No Quantity — the record simply associates a dunnage item with a WO/Sequence pair

  Model_SetupTech_SubordinatePart.cs
      ← WorkOrderId, SequenceNo, SubPartId, SubPartDesc, RequiredQty, QtyOnHand

  Model_SetupTech_DunnageTypeConfig.cs
      ← DunnageTypeId, DunnageTypeName, IsEnabled, DisplayOrder
         // Mirrors SetupTechDunnageTypeConfig; used to build category tabs in Step 4

Core/Models/InforVisual/
  Model_Visual_WorkOrderHeader.cs    ← OrderId, PartId, PartType, Status
  Model_Visual_WorkOrderSequence.cs  ← OrderId, SequenceNo, WorkCenterId
  Model_Visual_SubordinatePart.cs    ← OrderId, SequenceNo, PartId, PartDesc, RequiredQty, QtyOnHand
```

---

## New Interfaces / Services Needed

```
Core/Interfaces/SetupTech/
  IService_SetupTech.cs
  IRepository_SetupTechActiveJob.cs
  IRepository_SetupTechActiveJobLocal.cs
  IRepository_WorkOrderDunnage.cs
  IRepository_WorkOrderDunnageLocal.cs
  IRepository_SetupTechDunnageTypeConfig.cs       ← read-only; returns enabled types for Step 4 UI
  IRepository_SetupTechDunnageTypeConfigLocal.cs  ← cached locally for offline support

Core/Interfaces/InforVisual/
  IService_InforVisual.cs            ← shared with FEATURE-02; add GetWorkOrderHeaderAsync,
                                        GetWorkOrderSequencesAsync, GetSubordinatePartsAsync

Services/SetupTech/
  Service_SetupTech.cs               ← connectivity-aware dual-repository service

Data/Repositories/SetupTech/
  Repository_SetupTechActiveJob.cs   ← online via IApiClient
  Repository_SetupTechActiveJobLocal.cs
  Repository_WorkOrderDunnage.cs
  Repository_WorkOrderDunnageLocal.cs
  Repository_SetupTechDunnageTypeConfig.cs       ← calls usp_SetupTech_GetEnabledDunnageTypes
  Repository_SetupTechDunnageTypeConfigLocal.cs  ← sqlite-net-pcl cache for offline
```

---

## UI Implementation Notes

> ⚠️ **WinUI 3 only — not MAUI ContentPage, not generic XAML.**
>
> All Views for this feature must be implemented as **WinUI 3 pages** (`Microsoft.UI.Xaml` namespace), consistent with the rest of the Windows host application. Do **not** use MAUI `ContentPage`, MAUI layouts (`VerticalStackLayout`, `HorizontalStackLayout`, `Grid` from `Microsoft.Maui.Controls`), or generic WPF/Silverlight XAML constructs.
>
> The 10 mockup files in `Documents/Mockups/Feature07/` are **design reference only**. They were created in WPF-compatible XAML for the xaml.io previewer tool. When implementing Views, recreate each mockup's layout and visual intent using WinUI 3 controls and panels (`StackPanel`, `Grid`, `Border`, `Button`, `TextBlock`, `ComboBox`, `ScrollViewer`, etc. from `Microsoft.UI.Xaml.Controls`). The visual design, color palette, spacing, and component arrangement shown in the mockups must be faithfully reproduced.
>
> **Mockup → View mapping:**
> | Mockup file | View to implement |
> |---|---|
> | `Mockup_View_SetupTech_Step1_Workstation.Windows.xaml` | `View_Waitlist_SetupTech.Windows.xaml` (step 1 state) |
> | `Mockup_View_SetupTech_Step1_Workstation.Android.xaml` | `View_Waitlist_SetupTech.Android.xaml` (step 1 state) |
> | `Mockup_View_SetupTech_Step2_WorkOrder.Windows.xaml` | `View_Waitlist_SetupTech.Windows.xaml` (step 2 state) |
> | `Mockup_View_SetupTech_Step2_WorkOrder.Android.xaml` | `View_Waitlist_SetupTech.Android.xaml` (step 2 state) |
> | `Mockup_View_SetupTech_Step3_Validation.Windows.xaml` | `View_Waitlist_SetupTechValidation.Windows.xaml` |
> | `Mockup_View_SetupTech_Step3_Validation.Android.xaml` | `View_Waitlist_SetupTechValidation.Android.xaml` |
> | `Mockup_View_SetupTech_Step4_Dunnage.Windows.xaml` | `View_Waitlist_SetupTechDunnage.Windows.xaml` |
> | `Mockup_View_SetupTech_Step4_Dunnage.Android.xaml` | `View_Waitlist_SetupTechDunnage.Android.xaml` |
> | `Mockup_View_SetupTech_Step5_Confirmation.Windows.xaml` | `View_Waitlist_SetupTechConfirmation.Windows.xaml` |
> | `Mockup_View_SetupTech_Step5_Confirmation.Android.xaml` | `View_Waitlist_SetupTechConfirmation.Android.xaml` |

---

## XAML Files to Create

```
Feature.Waitlist/                          ← setup tech workflow lives in Feature.Waitlist
  Views/
    SetupTech/
      View_Waitlist_SetupTech.Windows.xaml           ← WinUI 3 — steps 1 & 2 wizard states
      View_Waitlist_SetupTech.Android.xaml           ← WinUI 3 — steps 1 & 2 wizard states
      View_Waitlist_SetupTech.xaml.cs
    SetupTechValidation/
      View_Waitlist_SetupTechValidation.Windows.xaml ← WinUI 3 — step 3 validation
      View_Waitlist_SetupTechValidation.Android.xaml ← WinUI 3 — step 3 validation
      View_Waitlist_SetupTechValidation.xaml.cs
    SetupTechDunnage/
      View_Waitlist_SetupTechDunnage.Windows.xaml    ← WinUI 3 — step 4 dunnage picker
      View_Waitlist_SetupTechDunnage.Android.xaml    ← WinUI 3 — step 4 dunnage picker
      View_Waitlist_SetupTechDunnage.xaml.cs
    SetupTechConfirmation/
      View_Waitlist_SetupTechConfirmation.Windows.xaml ← WinUI 3 — step 5 save confirmation
      View_Waitlist_SetupTechConfirmation.Android.xaml ← WinUI 3 — step 5 save confirmation
      View_Waitlist_SetupTechConfirmation.xaml.cs
  ViewModels/
    SetupTech/
      ViewModel_Waitlist_SetupTech.cs
    SetupTechValidation/
      ViewModel_Waitlist_SetupTechValidation.cs
    SetupTechDunnage/
      ViewModel_Waitlist_SetupTechDunnage.cs
    SetupTechConfirmation/
      ViewModel_Waitlist_SetupTechConfirmation.cs
```

### ViewModel — `ViewModel_Waitlist_SetupTech`

Key observable properties:
```csharp
[ObservableProperty] ObservableCollection<Model_VisualWorkcenter> _availableWorkcenters
[ObservableProperty] Model_VisualWorkcenter? _selectedWorkcenter
[ObservableProperty] string _workOrderInput               // barcode or manual entry
[ObservableProperty] int? _sequenceInput
[ObservableProperty] ObservableCollection<Model_Visual_WorkOrderSequence> _availableSequences
[ObservableProperty] Model_Visual_WorkOrderHeader? _workOrderHeader
[ObservableProperty] ObservableCollection<Model_Visual_SubordinatePart> _subordinateParts
[ObservableProperty] bool _isVisualOffline
[ObservableProperty] bool _isBusy
[ObservableProperty] int _wizardStep                      // 1–6
[ObservableProperty] string _errorMessage
```

Key commands:
```csharp
[RelayCommand] LoadWorkcentersAsync()
[RelayCommand] SelectWorkcenterAsync(Model_VisualWorkcenter wc)
[RelayCommand] ScanBarcodeAsync()                         // parses WO + Seq from barcode string
[RelayCommand] LookupWorkOrderAsync()
[RelayCommand] LoadSequencesAsync(string workOrderId)
[RelayCommand] ApproveWorkOrderAsync()
[RelayCommand] NavigateToDunnageAsync()
[RelayCommand] SaveActiveJobAsync()
```

---

## Windows vs Android Layout Notes

> All layout guidance below describes the *intent* from the mockups. Implement using **WinUI 3 controls** (`Microsoft.UI.Xaml`) — not MAUI controls.

**Windows (floor kiosk — WinUI 3):**
- Multi-column layout with stepper at top
- Barcode scanner input auto-focuses on step 2
- Validation screen: two-column card (Visual data left, subordinate parts right)
- Dunnage step: two-panel — existing assignment left, dunnage catalog picker right
- Sequence selector: `ComboBox` populated inline

**Android:**
- Single-column, step-by-step flow
- Full-width input field for barcode scanning
- Validation screen scrollable card
- Dunnage step: accordion-style list
- Minimum tap targets: 48 px

---

## Role Guard

This feature's entry point must verify the user's role before rendering. If the authenticated user is not `SetupTech`, `Admin`, or `Developer`, navigate back and show an access-denied message.

```csharp
// In ViewModel_Waitlist_SetupTech constructor or OnNavigatedToAsync
// Windows (WinUI 3): raise an event or use a navigation service — not Shell.Current
if (!_authService.CurrentUser.HasRole(Enum_UserRole.SetupTech) &&
    !_authService.CurrentUser.HasRole(Enum_UserRole.Admin) &&
    !_authService.CurrentUser.HasRole(Enum_UserRole.Developer))
{
    // WinUI 3 — navigate back via the MainWindow ShellFrame
    // Option A: inject INavigationService and call NavigateBack()
    // Option B: raise a NavigateBackRequested event the Page subscribes to
    _navigationService.GoBack();
    return;
}
```

> \u26a0\ufe0f `Shell.Current.GoToAsync("..")` is **Android (MAUI) only**. On Windows (WinUI 3), navigation is handled by `Frame.GoBack()` called from the Page, or via an injected `INavigationService`. The ViewModel raises a navigation event; the Page handles it.

---

## Open Decisions

| Question | Blocking | Owner |
|---|---|---|
| Who can log a press into a job — setup techs only, or also supervisors / leads? | Implementation | Dan, Nick |
| Should the barcode scanner call use the device camera or a USB wedge scanner on Windows kiosks? | Implementation | IT |
| Should subordinate-part cache (`WorkOrderSubordinateParts`) be invalidated after N hours, or only on next setup? | Implementation | Dan |
| Should dunnage assignments be global (any tech can set for a WO/Seq) or user-specific? Current spec is global. | Implementation | Dan |
| What `WORK_ORDER.STATUS` values are considered "active" for the sequence lookup? (See also FEATURE-02 open decisions.) | Implementation | Dan |
