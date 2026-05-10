# FEATURE-07: Setup Technician Module

**Status:** Design Required  
**Priority:** Medium  
**Depends On:** FEATURE-01, FEATURE-02 (Infor Visual), FEATURE-04 (queue)  
**Blocks:** Nothing  

---

## Overview

Setup technicians have a distinct workflow from both operators and material handlers. From the original meeting:

> *"You would enter the OP [sequence] when you set up the job, and then all they [operators] have to do is enter what they need. That is right. So they would just have their press number, they'd click it — because you already put the information in when you set up the job — the app would know exactly what it's running, and they could just click what they need."*

This is the key insight: **the setup tech is the source of truth for what work order and sequence a press is currently running.** When a setup tech "logs a job" to a press in this app, it creates a local record in MySQL that operators can then reference without typing anything from Infor Visual themselves.

---

## IMPORTANT

Upon the completion of this Feature the AI Agent is responsible for generating or updating any documentation files inside of .github/instructions, .github/copilot-instructions.md, AGENTS.MD, README.md (the core Readme file), as well as the creation of an md file we can later use when we get to the User Guide Phase of development.

---

## Core Concept: Press-to-WorkOrder Assignment

A setup tech starts their shift, walks to a press, and tells the app "Press 14 is now running Work Order WO-123456, Sequence 20." This is stored in a `PressAssignments` table in MySQL.

When an operator at Press 14 opens the request wizard (FEATURE-03), the app:
1. Reads the current `PressAssignments` record for Press 14
2. Pre-fills Work Order + Sequence automatically
3. Calls Infor Visual to validate (WL_01 + WL_02 queries) and get part info
4. Operator never types a work order number

This eliminates the root cause of the most costly error in the current system: wrong coils fetched because an operator misread a work order.

---

## Press Assignment Data Model

```sql
-- Tracks which work order and sequence is currently running on each press.
-- Updated by setup techs when they change a job.
CREATE TABLE PressAssignments (
    Id              INT UNSIGNED NOT NULL AUTO_INCREMENT,
    WorkcenterId    VARCHAR(100) NOT NULL,     -- SHOP_RESOURCE.ID from Infor Visual
    WorkOrderBaseId VARCHAR(50) NULL,          -- WORK_ORDER.BASE_ID
    SequenceNo      SMALLINT NULL,             -- OPERATION.SEQUENCE_NO
    SiteId          VARCHAR(20) NOT NULL,
    AssignedByUserId INT UNSIGNED NOT NULL,    -- the setup tech who set this
    AssignedAt      DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    ClearedAt       DATETIME NULL,             -- when job was done or press went idle
    UpdatedAt       DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    PRIMARY KEY pk_PressAssignments (Id),
    UNIQUE uq_PressAssignments_Workcenter_Active (WorkcenterId, ClearedAt),
    CONSTRAINT fk_PressAssignments_User FOREIGN KEY (AssignedByUserId) REFERENCES Users (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

Note: `UNIQUE` on `(WorkcenterId, ClearedAt)` where `ClearedAt IS NULL` ensures only one active assignment per press at a time. In MySQL 5.7 this requires a workaround (NULL values are not treated as equal in unique constraints) — use a sentinel value or a trigger to enforce uniqueness on active records.

---

## Setup Tech Workflow

### 1. Log a Press into a Job

1. Select workcenter (press) from list
2. App queries Infor Visual (`WL_03_GetActiveWorkOrdersForResource.sql`) — shows any work orders Visual says should be running on this press
3. Setup tech confirms or overrides the work order
4. Select the sequence (`SEQUENCE_NO` — the step they're setting up)
5. App validates via Visual (`WL_01` + `WL_02`)
6. "Log Job" — writes to `PressAssignments` via API → stored procedure

### 2. Clear a Press (Job Done)

When a job is complete or the press goes idle:
- Setup tech taps "Clear Press" on the press card
- `ClearedAt` is set to UTC now
- Press appears as "Idle" in the press board

### 3. Setup Tech Dashboard

A press board showing all workcenters and their current assignment status:

```
┌───────────────────────────────────────────────────────┐
│ PRESS BOARD — Site MTM2                               │
├────────────┬──────────────┬───────────────┬───────────┤
│ Workcenter │ Work Order   │ Sequence      │ Running   │
├────────────┼──────────────┼───────────────┼───────────┤
│ PRESS-14   │ WO-123456    │ Seq 20        │ 3h 12m    │
│ PRESS-07   │ WO-987654    │ Seq 10        │ 47m       │
│ PRESS-03   │ —            │ —             │ IDLE      │
│ PRESS-22   │ WO-112233    │ Seq 30        │ 1h 05m    │
└────────────┴──────────────┴───────────────┴───────────┘
```

Time in "Running" column = elapsed since `AssignedAt`.

---

## Setup Tech Waitlist Queue

Setup techs see a filtered queue showing only:
- `DieChange` requests
- `SetupAssistance` requests

Sorted by:
1. Scheduled sequence start time from Infor Visual (if available) — presses about to need a setup get priority
2. Time in queue (age)

Setup techs can claim and complete these tasks the same as material handlers claim their tasks.

---

## Infor Visual Integration

Setup techs benefit most from the Infor Visual integration:
- `WL_02_GetWorkOrderSequences.sql` — shows all sequences on a WO so the tech picks the right one (sequence numbers like 10, 20, 30 correspond to press operations)
- `WL_03_GetActiveWorkOrdersForResource.sql` — pre-suggests what should be running on the press per Visual's schedule

**Important:** The Visual data is advisory. Setup techs can override if Visual's schedule doesn't match the floor reality. The `PressAssignments` MySQL record is the source of truth for the operator wizard, not Visual.

---

## New ViewModel

```
Feature.Waitlist/
  ViewModels/
    Setup/
      ViewModel_Waitlist_Setup.cs

Feature.Mobile/
  ViewModels/
    Setup/
      ViewModel_Mobile_Setup.cs    (if a mobile-specific layout is needed)
```

Key properties:
```csharp
[ObservableProperty] ObservableCollection<Model_PressAssignment> _pressBoard
[ObservableProperty] ObservableCollection<Model_WaitlistEntry> _setupQueue
[ObservableProperty] Model_VisualWorkcenter? _selectedWorkcenter
[ObservableProperty] Model_VisualWorkOrder? _selectedWorkOrder
[ObservableProperty] Model_VisualWorkOrderSequence? _selectedSequence
[ObservableProperty] bool _isVisualOffline

[RelayCommand] LogJobAsync()
[RelayCommand] ClearPressAsync(Model_PressAssignment assignment)
[RelayCommand] ClaimSetupTaskAsync(Model_WaitlistEntry entry)
[RelayCommand] CompleteSetupTaskAsync(Model_WaitlistEntry entry)
```

---

## XAML Files

```
Feature.Waitlist/
  Views/
    Setup/
      View_Waitlist_Setup.Windows.xaml
      View_Waitlist_Setup.Android.xaml
      View_Waitlist_Setup.xaml.cs
```

---

## New API Endpoints

| Endpoint | Method | Description |
|---|---|---|
| `/api/pressassignments` | GET | Get all active press assignments for a site |
| `/api/pressassignments/{workcenterId}` | GET | Get current assignment for a specific press |
| `/api/pressassignments` | POST | Log a new job to a press |
| `/api/pressassignments/{id}/clear` | PUT | Clear a press (job done) |

---

## Open Decisions

- **Who can log a job?** Only `SetupTech` role, or also `ProductionSupervisor`? Recommendation: both, since supers sometimes cover setups.
- **Can an operator self-log?** For v1, no — only setup techs log jobs. If a press has no assignment, the operator wizard shows a message "No job logged to this press. Ask your setup tech to log the job, or enter work order manually."
- **What if a press has no Visual record?** Some auxiliary equipment may not appear in `SHOP_RESOURCE`. Allow setup techs to create a press assignment with `WorkOrderBaseId = null` to mark any press as "occupied" even without a WO.
- **Sequence display format:** The UI should show sequences as "Seq 10", "Seq 20", "Seq 30" matching what's printed on the physical work order. Never show the raw `SEQUENCE_NO` integer alone without the "Seq" label.
