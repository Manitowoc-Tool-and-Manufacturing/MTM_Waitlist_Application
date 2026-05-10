# FEATURE-04: Live Waitlist Queue View

**Status:** Ready to Design  
**Priority:** High — Core MVP Feature  
**Depends On:** FEATURE-01 (auth), FEATURE-03 (entries must exist to display)  
**Blocks:** FEATURE-05 (Material Handler needs queue to claim from)  

---

## Overview

The live queue is the equivalent of the current tablesready.com display. Every role sees the same underlying data but through a filtered, role-appropriate lens. This is the screen that's always open on floor kiosks — the one handlers glance at to know what's waiting.

---

## IMPORTANT

Upon the completion of this Feature the AI Agent is responsible for generating or updating any documentation files inside of .github/instructions, .github/copilot-instructions.md, AGENTS.MD, README.md (the core Readme file), as well as the creation of an md file we can later use when we get to the User Guide Phase of development.

---

## Role Views

### Operator View (`PressOperation`)

Shows only their own active requests.

| Column | Notes |
|---|---|
| Workcenter | Their press number |
| Request Type | Icon + label |
| Status | `Pending` / `InProgress` / `Completed` / `Late` |
| Submitted | Time elapsed since request (e.g., "12 min ago") |
| Handler | Who claimed it (once assigned) |

- Can cancel a request while Status = `Pending`
- Cannot see other operators' requests in this view (but a "Floor View" toggle could show all — future enhancement)

### Material Handler View (`MaterialHandler`)

Shows all pending/in-progress requests that are NOT `QualityInspection` and NOT `MaintenanceRequest`. Sorted by age descending (oldest at top — red when past time standard).

| Column | Notes |
|---|---|
| Workcenter | Where to go |
| Request Type | What to do |
| Work Order | WO + Sequence No (if provided) |
| Part | From Visual lookup (if WO provided) |
| Status | With time elapsed |
| Priority | Visual urgency indicator |

- **Claim a task:** tap to claim → Status → `InProgress`, assigned to self
- **Complete a task:** tap complete → Status → `Completed`
- **Quick Add:** dedicated button for logging tasks that didn't come from an operator request (handler proactively moved material)

### Production Lead / Supervisor View (`ProductionSupervisor`, `ProductionManager`)

Full queue — all request types, all operators, all handlers. Adds analytics column.

| Column | Notes |
|---|---|
| All handler view columns | + |
| Requested By | Which operator/workcenter submitted |
| Handler Assigned | Who has it |
| Time in Queue | Exact duration, color-coded by SLA |
| Action buttons | Can reassign, escalate, or cancel any entry |

### Quality View (`Quality`)

Shows only `QualityInspection` requests. Sorted by age. Shows workcenter, reason, and whether operator stated they can keep running.

### Setup Tech View (`SetupTech`)

Shows `DieChange` and `SetupAssistance` requests. Sorted by scheduled sequence start time from Visual (if available) to prioritize jobs about to start.

---

## Priority & Time Standard Coloring

From the meeting:
> *"Say a task is given 20 minutes to get done. It hits the five minute marker, it goes red."*

Each `RequestType` has a default time standard (stored in MySQL `RequestTypeTimeStandards` table — to be designed). The queue view calculates time elapsed since `RequestedAt` and colors the row:

| Time remaining | Color |
|---|---|
| > 50% of standard remaining | Green / neutral |
| < 50% of standard remaining | Yellow |
| < 25% of standard remaining (5-min warning) | Orange |
| Exceeded standard (past due) | Red — triggers auto-assign logic |

Status `Late` in `Enum_WaitlistStatus` matches this red state.

---

## Polling / Refresh Strategy

The app runs on a local network — real-time push isn't required for v1. Use polling:

- Queue view polls `Service_WaitlistEntry.GetAllEntriesAsync()` every **15 seconds** while the view is active
- Uses `CancellationToken` tied to the page's `Disappearing` event to stop polling when navigated away
- On status change (claim, complete, cancel) the local `ObservableCollection` is updated immediately (optimistic UI), then confirmed on next poll

**Future enhancement (v2):** SignalR hub on the API for true real-time push.

---

## ViewModel

```
ViewModels/
  Queue/
    ViewModel_Waitlist_Queue.cs
```

Key properties:
```csharp
[ObservableProperty] ObservableCollection<Model_WaitlistEntry> _entries
[ObservableProperty] bool _isBusy
[ObservableProperty] string _statusMessage
[ObservableProperty] bool _isPolling
[ObservableProperty] Enum_UserRole _currentUserRole   // controls which columns are visible
```

Key commands:
```csharp
[RelayCommand] StartPollingAsync()
[RelayCommand] StopPolling()
[RelayCommand] ClaimTaskAsync(Model_WaitlistEntry entry)
[RelayCommand] CompleteTaskAsync(Model_WaitlistEntry entry)
[RelayCommand] CancelRequestAsync(Model_WaitlistEntry entry)
[RelayCommand] ReassignTaskAsync(Model_WaitlistEntry entry)   // leads only
[RelayCommand] QuickAddAsync()                                // handlers only
[RelayCommand] RefreshAsync()                                 // manual pull-to-refresh
```

---

## XAML Files to Create

```
Feature.Waitlist/
  Views/
    Queue/
      View_Waitlist_Queue.Windows.xaml
      View_Waitlist_Queue.Android.xaml
      View_Waitlist_Queue.xaml.cs
  ViewModels/
    Queue/
      ViewModel_Waitlist_Queue.cs
```

---

## Windows Layout

- Full-width `CollectionView` with visible-column set controlled by role
- Top row: filter pills (All / Pending / In Progress / Late) + search box
- Role-based column visibility using `x:DataType` converters or separate `DataTemplate` per role
- Right-click context menu on row: Claim / Complete / Cancel / Reassign
- Top-right: "New Request" button (operators) or nothing (read-only leads who use analytics)

## Android Layout

- Card-based `CollectionView`, one card per entry
- Cards show: workcenter name large, request type icon, elapsed time
- Swipe right = Claim / Complete
- Swipe left = Cancel
- FAB (floating action button) = "New Request" for operators or "Quick Add" for handlers

---

## Quick Add (Material Handler)

From the meeting:
> *"Material handlers should be able to log tasks manually so they get credit for their work."*

Quick Add is a simplified non-wizard flow:
1. Select request type (what did you just do?)
2. Select workcenter (where did you do it?)
3. Work order (optional)
4. Notes (optional)
5. Submit — creates a `WaitlistEntry` with `Status = Completed` immediately (retroactive log)

This ensures handlers get credit in analytics for proactive material runs even when no operator formally requested it.

---

## New Database Lookup Table Needed

`RequestTypeTimeStandards` — a MySQL table that maps `Enum_WaitlistRequestType` values to a default response time in minutes. Adjustable by leads/admins without a code change. Needed before the color-coding logic can work.

```sql
CREATE TABLE RequestTypeTimeStandards (
    Id            INT UNSIGNED NOT NULL AUTO_INCREMENT,
    RequestType   ENUM('CoilDelivery','DunnageDelivery','PartPickup','DieChange',
                       'SetupAssistance','QualityInspection','MaintenanceRequest','ProjectTask')
                  NOT NULL,
    StandardMinutes SMALLINT UNSIGNED NOT NULL DEFAULT 20,
    CreatedAt     DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    UpdatedAt     DATETIME NOT NULL DEFAULT UTC_TIMESTAMP() ON UPDATE UTC_TIMESTAMP(),
    PRIMARY KEY pk_RequestTypeTimeStandards (Id),
    UNIQUE uq_RequestTypeTimeStandards_RequestType (RequestType)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## Open Decisions

- **Polling interval:** 15 seconds is a starting point. If the floor finds it too slow for urgent tasks, reduce to 5 seconds. If it causes server load concerns, increase to 30.
- **"Floor view" for operators:** Should operators be able to see all pending requests (not just their own) on a read-only display? Useful for kiosks mounted on the floor for at-a-glance status. This could be a third XAML layout (`View_Waitlist_FloorDisplay.Windows.xaml`) with no interaction, just a scrolling board.
- **RequestTypeTimeStandards management UI:** For v1, seed the table with reasonable defaults. A settings screen (future admin module) would let leads edit these without a database administrator.
- **Column sort:** Should columns be sortable in the Windows view? Useful for leads. Default sort is by `RequestedAt` descending (oldest at top, urgency-focused).
