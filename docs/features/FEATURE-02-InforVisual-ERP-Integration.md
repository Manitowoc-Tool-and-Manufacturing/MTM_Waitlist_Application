# FEATURE-02: Infor Visual ERP Integration (Read-Only)

**Status:** Ready to Design  
**Priority:** High — Required for Operator Wizard  
**Depends On:** FEATURE-01 (auth needed to know site context)  
**Blocks:** FEATURE-03 (Operator Request Wizard)  

---

## Overview

The operator wizard must validate work orders and auto-populate part and workcenter information from Infor Visual before the operator submits a waitlist request. This prevents the single most common error from the current system: handlers wasting time on wrong coils/parts because an operator misread a work order number.

The Receiving Application already has a working, tested pattern for connecting to Infor Visual (SQL Server `VISUAL\MTMFG`, `ApplicationIntent=ReadOnly`). This feature ports and extends that pattern for the queries the Waitlist Application needs.

**Hard constraint: READ ONLY. No INSERT / UPDATE / DELETE against Infor Visual. Ever.**

---

## IMPORTANT

Upon the completion of this Feature the AI Agent is responsible for generating or updating any documentation files inside of .github/instructions, .github/copilot-instructions.md, AGENTS.MD, README.md (the core Readme file), as well as the creation of an md file we can later use when we get to the User Guide Phase of development.

---

## Infor Visual Key Tables

All accessed via `Microsoft.Data.SqlClient` against `VISUAL\MTMFG` (SQL Server).

### `dbo.WORK_ORDER`

The top-level work order header.

| Column | Type | Notes |
|---|---|---|
| `BASE_ID` | nvarchar | Primary identifier — this is what operators see printed on work orders |
| `LOT_ID` | nvarchar | Usually `0` |
| `SPLIT_ID` | nvarchar | Usually `0` |
| `SUB_ID` | nvarchar | Sub-operation identifier |
| `PART_ID` | nvarchar | The part being manufactured |
| `STATUS` | nchar | `O` = Open, `R` = Released, `C` = Closed, `H` = Hold |
| `DESIRED_QTY` | decimal | Quantity ordered |
| `DESIRED_WANT_DATE` | datetime | Due date |
| `WAREHOUSE_ID` | nvarchar | Maps to site (e.g., `MTM2`) |

**Work order identifier:** Operators refer to work orders by `BASE_ID` only (e.g., `WO-123456`). The full key in Visual is `(TYPE, BASE_ID, LOT_ID, SPLIT_ID, SUB_ID)` but for lookups entering just `BASE_ID` is sufficient.

### `dbo.OPERATION`

Individual sequences (steps) on a work order. **Note: The database calls these sequences (`SEQUENCE_NO`), not operations.**

| Column | Type | Notes |
|---|---|---|
| `WORKORDER_BASE_ID` | nvarchar | FK to WORK_ORDER |
| `SEQUENCE_NO` | smallint | **The sequence number** — what the app and operators call the step/operation |
| `RESOURCE_ID` | nvarchar | The workcenter/press assigned to this sequence (FK to SHOP_RESOURCE.ID) |
| `STATUS` | nchar | `O` = Open, `C` = Closed, `S` = Scheduled |
| `SETUP_COMPLETED` | nchar | `Y`/`N` — whether setup is done |
| `SCHED_START_DATE` | datetime | Scheduled start |
| `SCHED_FINISH_DATE` | datetime | Scheduled finish |
| `COMPLETED_QTY` | decimal | How many pieces have run through this sequence |
| `CALC_END_QTY` | decimal | Target quantity at end of this sequence |

### `dbo.SHOP_RESOURCE`

The list of workcenters (presses, machines, benches).

| Column | Type | Notes |
|---|---|---|
| `ID` | nvarchar | Resource identifier — matches what operators know as their press number (e.g., `PRESS-14`) |
| `DESCRIPTION` | nvarchar | Human-readable name |
| `TYPE` | nchar | `M` = Machine, `L` = Labor, etc. |
| `SCHEDULE_GROUP_ID` | nvarchar | Can be used to filter by department/area |
| `DEPARTMENT_ID` | nvarchar | Department this resource belongs to |

### `dbo.PART`

Part master.

| Column | Type | Notes |
|---|---|---|
| `ID` | nvarchar | Part number |
| `DESCRIPTION` | nvarchar | Part description |
| `STOCK_UM` | nvarchar | Unit of measure |

### `dbo.PART_SITE`

Per-site inventory.

| Column | Type | Notes |
|---|---|---|
| `PART_ID` | nvarchar | FK to PART |
| `SITE_ID` | nvarchar | Site (e.g., `MTM2`) |
| `QTY_ON_HAND` | decimal | Current stock |
| `PRIMARY_LOC_ID` | nvarchar | Default storage location |

---

## Queries Needed

These are new queries not present in the Receiving Application. Files go in `database/infor_visual/queries/`.

### `WL_01_GetWorkOrderHeader.sql`

Validate a work order exists and get its part + status:

```sql
-- WL_01_GetWorkOrderHeader.sql
-- Get work order header by BASE_ID (what operators see on their work order).
-- Returns the part being run, its description, and the WO status.
-- READ ONLY — Infor Visual MTMFG
SELECT
    wo.BASE_ID          AS WorkOrderId,
    wo.PART_ID          AS PartId,
    p.DESCRIPTION       AS PartDescription,
    wo.STATUS           AS WorkOrderStatus,
    wo.DESIRED_QTY      AS DesiredQty,
    wo.DESIRED_WANT_DATE AS WantDate,
    wo.WAREHOUSE_ID     AS SiteId
FROM dbo.WORK_ORDER wo
LEFT JOIN dbo.PART p ON wo.PART_ID = p.ID
WHERE wo.BASE_ID = @WorkOrderBaseId
  AND wo.TYPE = 'W'           -- standard work orders only
ORDER BY wo.LOT_ID, wo.SPLIT_ID, wo.SUB_ID;
```

### `WL_02_GetWorkOrderSequences.sql`

Get all sequences (steps) for a work order so the operator can pick which sequence they are currently running:

```sql
-- WL_02_GetWorkOrderSequences.sql
-- Get all sequences on a work order.
-- SEQUENCE_NO is what operators and leads call the "operation" or "step".
-- RESOURCE_ID is the workcenter (press) assigned to this sequence.
-- READ ONLY — Infor Visual MTMFG
SELECT
    op.WORKORDER_BASE_ID    AS WorkOrderId,
    op.SEQUENCE_NO          AS SequenceNo,
    op.RESOURCE_ID          AS WorkcenterId,
    sr.DESCRIPTION          AS WorkcenterDescription,
    op.STATUS               AS SequenceStatus,
    op.SETUP_COMPLETED      AS SetupCompleted,
    op.COMPLETED_QTY        AS CompletedQty,
    op.CALC_END_QTY         AS TargetQty,
    op.SCHED_START_DATE     AS SchedStart,
    op.SCHED_FINISH_DATE    AS SchedFinish
FROM dbo.OPERATION op
LEFT JOIN dbo.SHOP_RESOURCE sr ON op.RESOURCE_ID = sr.ID
WHERE op.WORKORDER_BASE_ID = @WorkOrderBaseId
  AND op.WORKORDER_TYPE = 'W'
ORDER BY op.SEQUENCE_NO;
```

### `WL_03_GetActiveWorkOrdersForResource.sql`

Given a workcenter/press ID, what work orders are currently active on it? Used when a setup tech logs the press into a job:

```sql
-- WL_03_GetActiveWorkOrdersForResource.sql
-- Find open/released work order sequences assigned to a specific workcenter.
-- Helps setup techs confirm which WO is currently running on a press.
-- READ ONLY — Infor Visual MTMFG
SELECT
    op.WORKORDER_BASE_ID    AS WorkOrderId,
    wo.PART_ID              AS PartId,
    p.DESCRIPTION           AS PartDescription,
    op.SEQUENCE_NO          AS SequenceNo,
    op.STATUS               AS SequenceStatus,
    op.COMPLETED_QTY        AS CompletedQty,
    op.CALC_END_QTY         AS TargetQty,
    wo.DESIRED_WANT_DATE    AS WantDate
FROM dbo.OPERATION op
INNER JOIN dbo.WORK_ORDER wo
    ON wo.BASE_ID = op.WORKORDER_BASE_ID
    AND wo.TYPE = op.WORKORDER_TYPE
LEFT JOIN dbo.PART p ON wo.PART_ID = p.ID
WHERE op.RESOURCE_ID = @ResourceId
  AND op.WORKORDER_TYPE = 'W'
  AND op.STATUS IN ('O', 'R', 'S')      -- Open, Released, Scheduled
  AND wo.STATUS IN ('O', 'R')
ORDER BY wo.DESIRED_WANT_DATE, op.SEQUENCE_NO;
```

### `WL_04_GetAllWorkcenters.sql`

List all active machine/labor resources for the press floor. Cached on startup — used to populate workcenter dropdowns without a Visual round-trip on every request:

```sql
-- WL_04_GetAllWorkcenters.sql
-- Load all shop resources (workcenters/presses) for the press floor.
-- Cached at app startup. Updated on demand.
-- READ ONLY — Infor Visual MTMFG
SELECT
    sr.ID               AS WorkcenterId,
    sr.DESCRIPTION      AS Description,
    sr.TYPE             AS ResourceType,
    sr.DEPARTMENT_ID    AS DepartmentId,
    sr.SCHEDULE_GROUP_ID AS ScheduleGroup
FROM dbo.SHOP_RESOURCE sr
WHERE sr.SCHEDULE_NORMALLY = 'Y'
ORDER BY sr.ID;
```

---

## C# Architecture

### New DAO: `Dao_InforVisualWorkOrder`

Mirrors the pattern from Receiving's `Dao_InforVisualConnection`. Placed in `MTM_Waitlist_Application.Data`:

```
Data/
  InforVisual/
    Dao_InforVisualWorkOrder.cs    ← NEW
```

Constructor takes `connectionString` (from `appsettings.json`), enforces `ApplicationIntent=ReadOnly`.

Methods:
- `GetWorkOrderHeaderAsync(string workOrderBaseId)` → `Model_Dao_Result<Model_VisualWorkOrder>`
- `GetWorkOrderSequencesAsync(string workOrderBaseId)` → `Model_Dao_Result<List<Model_VisualWorkOrderSequence>>`
- `GetActiveWorkOrdersForResourceAsync(string resourceId)` → `Model_Dao_Result<List<Model_VisualWorkOrder>>`
- `GetAllWorkcentersAsync()` → `Model_Dao_Result<List<Model_VisualWorkcenter>>`

### New Models (Core)

```
Core/Models/InforVisual/
  Model_VisualWorkOrder.cs          ← WorkOrderId, PartId, PartDescription, Status, DesiredQty, WantDate, SiteId
  Model_VisualWorkOrderSequence.cs  ← WorkOrderId, SequenceNo, WorkcenterId, WorkcenterDescription, SequenceStatus, SetupCompleted, CompletedQty, TargetQty
  Model_VisualWorkcenter.cs         ← WorkcenterId, Description, ResourceType, DepartmentId, ScheduleGroup
```

### New Service: `IService_InforVisual` / `Service_InforVisual`

Placed in `MTM_Waitlist_Application.Services`:

```
Services/
  InforVisual/
    IService_InforVisual.cs    ← NEW (Core/Interfaces/InforVisual/)
    Service_InforVisual.cs     ← NEW
```

Responsibilities:
- Wraps `Dao_InforVisualWorkOrder`
- **Workcenter cache:** loads all workcenters once at startup, refreshes on demand. Prevents hitting SQL Server on every operator request.
- **Work order validation:** checks `STATUS` and returns a `ValidationResult` with human-readable messages
- **Graceful offline handling:** if Visual is unreachable, returns a `Model_Dao_Result` with `IsSuccess = false` and `ErrorMessage` — the operator wizard shows a warning but does NOT block the request (they can still submit with manually entered data)

### Connection String

Add to `appsettings.json`:

```json
"ConnectionStrings": {
  "InforVisual": "Server=VISUAL;Database=MTMFG;Integrated Security=True;ApplicationIntent=ReadOnly;Connect Timeout=5;Command Timeout=10;"
}
```

Note: `Integrated Security=True` requires the app to run under a domain account that has read access to the Visual database. The Receiving Application uses this same pattern and it works today.

---

## DI Registration

In `MauiProgramExtensions.cs`, `AddSharedServices()`:

```csharp
// Infor Visual (READ-ONLY SQL Server) — Singleton, stateless DAO
services.AddSingleton<Dao_InforVisualWorkOrder>(sp =>
{
    var cs = configuration.GetConnectionString("InforVisual")
             ?? throw new InvalidOperationException("InforVisual connection string missing");
    var logger = sp.GetRequiredService<ILogger<Dao_InforVisualWorkOrder>>();
    return new Dao_InforVisualWorkOrder(cs, logger);
});
services.AddSingleton<IService_InforVisual, Service_InforVisual>();
```

---

## Workcenter Name = WorkcenterName on WaitlistEntry

The `WaitlistEntries.WorkcenterName` column stores the `SHOP_RESOURCE.ID` value (e.g., `PRESS-14`). When an operator selects their press, they pick from the workcenter list pulled from Infor Visual. `Service_InforVisual` maps the `ID` → display name for UI but stores `ID` in the waitlist entry. This means even if a workcenter description changes in Visual, historical entries remain coherent.

---

## Files to Create

| File | Project |
|---|---|
| `database/infor_visual/queries/WL_01_GetWorkOrderHeader.sql` | (repo root docs) |
| `database/infor_visual/queries/WL_02_GetWorkOrderSequences.sql` | (repo root docs) |
| `database/infor_visual/queries/WL_03_GetActiveWorkOrdersForResource.sql` | (repo root docs) |
| `database/infor_visual/queries/WL_04_GetAllWorkcenters.sql` | (repo root docs) |
| `Core/Models/InforVisual/Model_VisualWorkOrder.cs` | Core |
| `Core/Models/InforVisual/Model_VisualWorkOrderSequence.cs` | Core |
| `Core/Models/InforVisual/Model_VisualWorkcenter.cs` | Core |
| `Core/Interfaces/InforVisual/IService_InforVisual.cs` | Core |
| `Data/InforVisual/Dao_InforVisualWorkOrder.cs` | Data |
| `Services/InforVisual/Service_InforVisual.cs` | Services |

---

## Open Decisions

- **Connection string auth:** `Integrated Security=True` works on domain workstations. If some floor kiosks are NOT domain-joined, a SQL login fallback is needed. This is the same tradeoff the Receiving App faces.
- **Workcenter filter:** `SCHEDULE_NORMALLY = 'Y'` is a guess — confirm with Dan whether all active presses have this flag set or if a different filter is needed.
- **Work order STATUS filter:** The query above shows `O` (Open) and `R` (Released) work orders as "active". Confirm whether `H` (Hold) work orders should be shown as a warning or blocked entirely.
- **SITE_ID scoping:** The Receiving App queries Visual with `SITE_ID = 'MTM2'`. Confirm the correct site ID(s) for the Waitlist app locations.
