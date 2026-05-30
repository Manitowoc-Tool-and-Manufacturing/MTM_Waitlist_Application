# FEATURE-06: Lead Analytics & Dashboard

**Status:** Design Required  
**Priority:** Medium — Phase 2 after MVP Queue is Working  
**Depends On:** FEATURE-01, FEATURE-03, FEATURE-04, FEATURE-05  
**Blocks:** Nothing  

---

## Overview

Production leads and supervisors have analytics rights that operators and handlers do not. From the original meeting:

> *"You would be able to see what your operators are putting on — if they're abusing it."*  
> *"If you came to me and said I want to see what operators are currently logged into a job, I can add that to your module."*  
> *"There is a preset time for each job type, which can be adjusted by Brett or Nick based on data collected from the app."*

The existing `View_Dashboard_Main` is a placeholder with no real data. This feature replaces its placeholder data with live analytics and adds the lead-specific management tools.

---

## IMPORTANT

Upon the completion of this Feature the AI Agent is responsible for generating or updating any documentation files inside of .github/instructions, .github/copilot-instructions.md, AGENTS.MD, README.md (the core Readme file), as well as the creation of an md file we can later use when we get to the User Guide Phase of development.

---

## What Leads and Supervisors Need

### 1. Live Floor Status

At a glance, across the entire site:
- How many active waitlist requests are open right now?
- How many are in the red (past time standard)?
- Which workcenters have been waiting longest?
- Which handlers are currently working, and on what?

This is a real-time snapshot — updated on the same polling interval as the queue view (15 seconds).

### 2. Time-in-Queue Analytics

From the meeting, the core analytics ask: how long does each request type take to complete?

Metrics per request type:
- Average time from `RequestedAt` to `CompletedAt`
- Average time from `RequestedAt` to `ScheduledAt` (time to first handler assignment)
- Count of requests completed within SLA vs. past SLA
- Trend over time (daily / weekly)

Metrics per handler:
- Tasks completed per shift
- Average completion time
- Tasks returned / rejected
- Quick Adds logged (proactive work credit)

Metrics per workcenter:
- Request frequency by type
- Average queue wait time
- Whether certain presses have disproportionate requests (possible equipment or training issue)

### 3. Zone Assignment Management

Leads can override or set handler zone assignments:
- See which handlers are assigned to which zones today
- Reassign a handler to a different zone
- See which zones have no handlers assigned (coverage gap)

### 4. Task Override Controls

- Reassign any in-progress task to a different handler
- Cancel any task (with reason)
- Escalate a task (marks it as high priority, moves it to top of queue)
- Set a task's status back to Pending (handler claimed but hasn't started)

### 5. Time Standard Management

The per-request-type SLA values stored in `RequestTypeTimeStandards` must be editable by leads without requiring a database administrator. A simple settings form:
- Select request type
- Enter new standard in minutes
- Save (calls API → stored procedure)

---

## Dashboard Cards (Updated from Placeholder)

The existing `View_Dashboard_Main` has 4 placeholder summary cards. Replace with:

| Card | Metric | Source |
|---|---|---|
| Open Requests | Count of `Status IN (Pending, InProgress)` | API |
| Overdue (Red) | Count of `Status = Late OR time > standard` | API |
| Handlers Active | Count of handlers with `Status = InProgress` tasks | API |
| Avg Completion Today | Average minutes from request to complete (today) | API |

Below the cards: two panels
- Left: Live queue (same as FEATURE-04 queue but lead view — all columns visible)
- Right: Handler status board (each handler, their current zone, current task, idle/active)

---

## New API Endpoints Needed

The current API has only CRUD on `/api/waitlist`. Analytics require additional endpoints:

| Endpoint | Method | Description |
|---|---|---|
| `/api/analytics/summary` | GET | Counts for dashboard cards |
| `/api/analytics/by-handler` | GET | Per-handler metrics, date range |
| `/api/analytics/by-requesttype` | GET | Per-type metrics, date range |
| `/api/analytics/by-workcenter` | GET | Per-workcenter metrics, date range |
| `/api/zones` | GET/POST/PUT | Zone CRUD |
| `/api/zones/assignments` | GET/POST/PUT | Handler zone assignment management |
| `/api/settings/time-standards` | GET/PUT | SLA time standard management |

---

## New Stored Procedures Needed

```
Database/procedures/Analytics/
  usp_Analytics_GetSummary.sql
  usp_Analytics_GetByHandler.sql
  usp_Analytics_GetByRequestType.sql
  usp_Analytics_GetByWorkcenter.sql

Database/procedures/Zones/
  usp_Zone_GetAll.sql
  usp_Zone_GetHandlerAssignments.sql
  usp_Zone_UpsertHandlerAssignment.sql
```

---

## ViewModel

The existing `ViewModel_Dashboard_Main` handles the 4-card summary. This feature extends it:

```csharp
// Current (placeholder)
[ObservableProperty] bool _isBusy;
[ObservableProperty] string _statusMessage;

// Add for Feature-06:
[ObservableProperty] int _openRequestCount;
[ObservableProperty] int _overdueRequestCount;
[ObservableProperty] int _activeHandlerCount;
[ObservableProperty] double _avgCompletionMinutesToday;
[ObservableProperty] ObservableCollection<Model_WaitlistEntry> _liveQueue;
[ObservableProperty] ObservableCollection<Model_HandlerStatus> _handlerBoard;

[RelayCommand] RefreshAnalyticsAsync();
[RelayCommand] ReassignTaskAsync(Model_WaitlistEntry entry);
[RelayCommand] EscalateTaskAsync(Model_WaitlistEntry entry);
[RelayCommand] UpdateTimeStandardAsync(RequestTypeStandard standard);
```

A separate VM handles the deep analytics tab:

```
ViewModels/
  Analytics/
    ViewModel_Dashboard_Analytics.cs
```

---

## XAML Changes

- `View_Dashboard_Main.Windows.xaml` — replace 4 placeholder cards with live-data binding
- `View_Dashboard_Main.Android.xaml` — same, single-column card stack
- New: `View_Dashboard_Analytics.Windows.xaml` + `.Android.xaml` for the detailed drill-down charts

---

## Visual ERP Integration for Analytics

Leads requested the ability to see which operators are "currently logged into a job" — meaning which operators have active Visual work orders at their workcenters. This is a read from Infor Visual:

Call `Service_InforVisual.GetActiveWorkOrdersForResourceAsync(workcenterId)` for each active workcenter. Cross-reference with waitlist entries to show: *"Press 14 — WO-123456 — currently has a Coil Delivery request waiting 22 min."*

This gives leads a single screen to see the connection between Visual work orders and waitlist activity without toggling between systems.

---

## Open Decisions

- **Chart library:** Neither WinUI 3 nor .NET MAUI include a built-in chart control. For the analytics drill-down screen, options include: `LiveCharts2.WinUI` (Windows, rich, commercial), `LiveCharts2.Maui` (Android), `Microcharts` (simple, MIT, both platforms), or simple `ListView`/table views for v1. Recommendation: v1 uses data tables only. Charts are a v2 enhancement.
- **Analytics date range:** Default to today. Should also support last 7 days and last 30 days. A date picker control is needed on Windows; a simpler "Today / Week / Month" segmented control on Android.
- **Who sees analytics:** `ProductionSupervisor` and `ProductionManager` see full analytics. `SetupTech` and `Quality` see their own queue metrics only (how long requests in their category take). `MaterialHandler` does NOT see analytics — only their own completed task history.
- **Export:** Leads asked about data export (implied in the meeting: "data trail for project management"). For v1, no export. For v2, CSV export of the analytics tables.
