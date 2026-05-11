# FEATURE-05: Material Handler Module (Zone Assignment & Task Management)

**Status:** Design Required Before Implementation  
**Priority:** Medium-High — Required for Full MVP  
**Depends On:** FEATURE-01 (auth), FEATURE-04 (queue must exist)  
**Blocks:** Nothing  

---

## Overview

Material handlers are the primary consumers of waitlist entries. This feature gives them the tools to manage their workload by zone, claim tasks, complete them, and log proactive work for credit. The meeting had substantial discussion specifically about material handler pain points with the current tablesready.com system.

Key pain points from the meeting:
> *"People are hand-picking tasks. They're not grabbing coils. They're not grabbing dies."*  
> *"Press operators are not going to be tech savvy. We have to be very simplified."*  
> *"Is it going to have a recents where they don't have to go through [the whole form] again?"*  
> *"If they're asking for skids, then Gaylords — they do pick up skids Gaylords all on one line, instead of three separate transactions."* (solved by FEATURE-03 multi-select)

---

## IMPORTANT

Upon the completion of this Feature the AI Agent is responsible for generating or updating any documentation files inside of .github/instructions, .github/copilot-instructions.md, AGENTS.MD, README.md (the core Readme file), as well as the creation of an md file we can later use when we get to the User Guide Phase of development.

---

## Zone System

### What Zones Are

Zones are named areas of the press floor. A material handler assigned to a zone is the primary responder for tasks originating from workcenters in that zone. This prevents handlers picking easy tasks and ignoring urgent coil moves across the plant.

### Zone Configuration (Lead-managed)

- Zones are defined in a MySQL `Zones` table: `ZoneId`, `ZoneName`, `SiteId`
- Workcenters are mapped to zones in a `WorkcenterZones` table: `WorkcenterId`, `ZoneId`
  - `WorkcenterId` = `SHOP_RESOURCE.ID` from Infor Visual
- Material handlers are assigned to zones at shift start in a `HandlerZoneAssignments` table: `UserId`, `ZoneId`, `AssignedAt`, `ShiftDate`
- A handler can be assigned to multiple zones
- Zone assignments are per-shift, not permanent

### Auto-Assignment Logic (Phase 1 — Simple)

For v1, auto-assignment is implemented as a server-side suggestion only (not enforced). When a task goes into the red (past time standard):

1. Find handlers assigned to the zone where the requesting workcenter is located
2. Among those, pick the one with the fewest active (`InProgress`) tasks
3. Set `AssignedToUserId` on the `WaitlistEntry` — the handler's queue view will highlight it

Handlers can still decline (claim a different task), but their lead can see the auto-assignment was overridden via audit log.

**From the meeting — forced assignment:** The meeting discussed auto-assign as non-optional when a task goes red. For v1, this is a strong visual nudge, not a hard lock. The meeting's consensus was to trial nudge-based first and evaluate.

---

## Material Handler Dashboard (`Feature.Mobile` + Windows)

### Main Screen Layout (Android — primary platform for handlers)

```
┌─────────────────────────────────────────────┐
│  [Zone: Zone A]            [Quick Add]  [≡] │
├─────────────────────────────────────────────┤
│  MY TASKS (2)                               │
│  ┌─────────────────────────────────────────┐│
│  │ PRESS-14 · Coil Delivery · 22 min 🔴   ││
│  │ WO-123456 · Seq 20                     ││
│  │ [START]  [COMPLETE]                     ││
│  └─────────────────────────────────────────┘│
│  ┌─────────────────────────────────────────┐│
│  │ PRESS-07 · Dunnage · 8 min              ││
│  │ Skids (2)                               ││
│  │ [START]  [COMPLETE]                     ││
│  └─────────────────────────────────────────┘│
├─────────────────────────────────────────────┤
│  AVAILABLE IN MY ZONES (5)                  │
│  [PRESS-22 · Coil · 14 min] [CLAIM]        │
│  [PRESS-03 · Dunnage · 3 min] [CLAIM]      │
│  ...                                        │
└─────────────────────────────────────────────┘
```

All tap targets: minimum 64 px height on Android.

### Main Screen Layout (Windows — floor kiosk)

- Split panel: left = My Tasks, right = Available in Zone
- Status bar showing current zone assignment
- Handler can change zone assignment from a settings fly-in (requires lead approval in future — for v1 it's self-service)

---

## Task Lifecycle from Handler Perspective

```
[Pending] → [Claimed/InProgress] → [Completed]
                ↓
           [Returned to Queue]  (handler can't complete — e.g., equipment down)
```

### Start a Task

- Handler taps Claim on an available task
- Status → `InProgress`, `AssignedToUserId` = handler's ID
- Timer starts (tracked by `RequestedAt` still; `ScheduledAt` = time handler claimed it)

### Complete a Task

- Handler taps Complete
- Status → `Completed`, `CompletedAt` = UTC now
- Entry is removed from active queue (moves to history)
- Elapsed time is logged for analytics

### Return / Reject a Task

If a handler cannot complete (forklift down, wrong info, safety issue):
- Handler taps Return → required: select a reason from a short list
- Status → `Pending` again, `AssignedToUserId` = null
- Reason logged in audit trail (or a `Notes` append)
- Lead is notified in their analytics view that a task was returned

---

## Quick Add (Retroactive Logging)

From the meeting:
> *"Material handlers should be able to log tasks manually so they get credit for their work."*

Quick Add is a short form:
1. What did you do? (request type)
2. Which workcenter? (dropdown)
3. Work order? (optional, Visual lookup)
4. Notes? (optional)

Submits as a `WaitlistEntry` with `Status = Completed` immediately — no queue time, pure credit/history log.

---

## New Database Tables Needed

```sql
-- Zones (press floor area definitions)
CREATE TABLE Zones (
    Id        INT UNSIGNED NOT NULL AUTO_INCREMENT,
    ZoneName  VARCHAR(50) NOT NULL,
    SiteId    VARCHAR(20) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    UpdatedAt DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    PRIMARY KEY pk_Zones (Id),
    UNIQUE uq_Zones_ZoneName_SiteId (ZoneName, SiteId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- WorkcenterZones (which workcenters are in which zone)
-- WorkcenterId is SHOP_RESOURCE.ID from Infor Visual (read-only lookup)
CREATE TABLE WorkcenterZones (
    Id          INT UNSIGNED NOT NULL AUTO_INCREMENT,
    WorkcenterId VARCHAR(100) NOT NULL,
    ZoneId      INT UNSIGNED NOT NULL,
    CreatedAt   DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    UpdatedAt   DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    PRIMARY KEY pk_WorkcenterZones (Id),
    UNIQUE uq_WorkcenterZones_Workcenter_Zone (WorkcenterId, ZoneId),
    CONSTRAINT fk_WorkcenterZones_Zone FOREIGN KEY (ZoneId) REFERENCES Zones (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- HandlerZoneAssignments (which handler is in which zone today)
CREATE TABLE HandlerZoneAssignments (
    Id          INT UNSIGNED NOT NULL AUTO_INCREMENT,
    UserId      INT UNSIGNED NOT NULL,
    ZoneId      INT UNSIGNED NOT NULL,
    ShiftDate   DATE NOT NULL,
    AssignedAt  DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    AssignedByUserId INT UNSIGNED NULL,     -- lead who made the assignment (null = self-assigned)
    UpdatedAt   DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    PRIMARY KEY pk_HandlerZoneAssignments (Id),
    CONSTRAINT fk_HandlerZoneAssignments_User FOREIGN KEY (UserId) REFERENCES Users (Id),
    CONSTRAINT fk_HandlerZoneAssignments_Zone FOREIGN KEY (ZoneId) REFERENCES Zones (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## New Core Models Needed

```
Core/Models/Waitlist/
  Model_Zone.cs                    ← ZoneId, ZoneName, SiteId
  Model_HandlerZoneAssignment.cs   ← AssignmentId, UserId, ZoneId, ZoneName, ShiftDate
```

---

## New Interfaces / Services Needed

```
Core/Interfaces/Waitlist/
  IService_Zone.cs
  IRepository_Zone.cs
  IRepository_HandlerZoneAssignment.cs

Services/Waitlist/
  Service_Zone.cs

Data/Repositories/Waitlist/
  Repository_Zone.cs
  Repository_ZoneLocal.cs
  Repository_HandlerZoneAssignment.cs
  Repository_HandlerZoneAssignmentLocal.cs
```

---

## XAML Files

This feature primarily lives in `Feature.Mobile` for the Android handler experience. The Windows version extends the existing queue view in `Feature.Waitlist`.

```
Feature.Mobile/
  Views/
    HandlerDashboard/
      View_Mobile_HandlerDashboard.Android.xaml
      View_Mobile_HandlerDashboard.Windows.xaml
      View_Mobile_HandlerDashboard.xaml.cs
    QuickAdd/
      View_Mobile_QuickAdd.Android.xaml
      View_Mobile_QuickAdd.Windows.xaml
      View_Mobile_QuickAdd.xaml.cs
  ViewModels/
    HandlerDashboard/
      ViewModel_Mobile_HandlerDashboard.cs
    QuickAdd/
      ViewModel_Mobile_QuickAdd.cs
```

---

## Shift Start Flow (Zone Assignment)

When a material handler first opens the app after logging in, if they have no zone assignment for today:
1. Show "Select Your Zone" screen listing all zones for their site
2. They tap their zone(s)
3. Assignment is saved — they proceed to the handler dashboard

This replaces the "lead assigns zones" model from the meeting. For v1, handlers self-assign. Leads can override via the analytics/admin screen (FEATURE-06).

---

## Open Decisions

- **Forced auto-assignment vs. nudge:** The meeting discussed forcing assignment when a task goes red. The spec above is a nudge (visual highlight + alert). Leads should decide if forced assignment (handler cannot skip their zone's red task) is actually desirable before it's implemented.
- **Rejection reasons list:** A short hardcoded list for v1 (Equipment down / Wrong information / Safety issue / Other). Should become a configurable lookup table in v2.
- **Handler seeing all zones:** Can a handler with no zone assignment see the full queue? For v1, yes — they see everything unfiltered. Proper zone filtering only applies when they have an active assignment.
- **Multiple zone assignments:** The schema allows it. The UI should allow selecting multiple zones at shift start.
