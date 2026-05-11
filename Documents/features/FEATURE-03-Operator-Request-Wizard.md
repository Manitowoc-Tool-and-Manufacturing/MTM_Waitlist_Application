# FEATURE-03: Operator Request Wizard (Feature.Waitlist)

**Status:** Ready to Design  
**Priority:** High — Core MVP Feature  
**Depends On:** FEATURE-01 (auth), FEATURE-02 (Infor Visual)  
**Blocks:** FEATURE-04 (Live Queue View), FEATURE-05 (Material Handler)  

---

## Overview

This is the primary screen for press operators. It replaces the current tablesready.com workflow. The goal from the original meeting is clear:

> *"My goal is to make it to where the operator does not type the damn thing unless they absolutely have to — they click from dropdown menus."*

The wizard pre-populates as much as possible from Infor Visual, then presents only the specific choices needed for the request type.

---

## IMPORTANT

Upon the completion of this Feature the AI Agent is responsible for generating or updating any documentation files inside of .github/instructions, .github/copilot-instructions.md, AGENTS.MD, README.md (the core Readme file), as well as the creation of an md file we can later use when we get to the User Guide Phase of development.

---

## Confirmed Request Types (`Enum_WaitlistRequestType`)

Already defined in the Core project. Eight values:

| Enum Value | What it means |
|---|---|
| `CoilDelivery` | New coil/metal stock needed at press |
| `DunnageDelivery` | Containers, pallets, Gaylords, skids |
| `PartPickup` | Finished parts need to be moved |
| `DieChange` | Setup tech needed to change the die |
| `SetupAssistance` | Setup tech help (not a full die change) |
| `QualityInspection` | Quality technician needed at the press |
| `MaintenanceRequest` | Maintenance/repair needed |
| `ProjectTask` | Long-running project work (not a quick task) |

**From the meeting:** Operators confirmed they frequently need coils, dunnage (skids, Gaylords, boxes), and quality checks as their most common requests. The wizard flow adapts to the selected request type.

---

## Operator Wizard Flow

### Step 1 — Select Workcenter

The operator selects their press from a list. This list is pulled from `Service_InforVisual.GetAllWorkcentersAsync()` (cached — no Visual round-trip on every use).

- **Default:** if the device has a previously-used workcenter saved in `LocalDbContext`, pre-select it
- **Windows UI:** scrollable list or dropdown with search
- **Android UI:** large tap-target list, one workcenter per row

### Step 2 — Work Order Context (Optional but Encouraged)

After selecting a workcenter, the app calls `Service_InforVisual.GetActiveWorkOrdersForResourceAsync(workcenterId)` and shows any currently running work orders. The operator taps to confirm which work order they are running.

- If no active work orders found in Visual → show empty state with message "No active work orders found for this workcenter. You can continue without a work order."
- If the operator enters a work order manually → call `GetWorkOrderHeaderAsync()` to validate it
- If Visual is offline → show warning "Visual ERP is offline — work order validation unavailable. You can still submit your request."
- The `SEQUENCE_NO` (what they call the operation/step) is selected from the sequences on the work order, shown as a dropdown. Label: **"Sequence"** (matching the database field name and what the team calls it).

### Step 3 — Select Request Type

Large button grid showing all applicable request types for the current user's role. `PressOperation` role sees: `CoilDelivery`, `DunnageDelivery`, `PartPickup`, `DieChange`, `SetupAssistance`, `QualityInspection`, `MaintenanceRequest`. `ProjectTask` is for leads/setup techs only.

**From the meeting — multi-select dunnage:** If the operator selects `DunnageDelivery`, they see a checklist of specific items (skids, Gaylords, boxes, etc.). Each checked item becomes a **separate** `WaitlistEntry` row — one transaction per item type. This was specifically requested to avoid the current problem of handlers only completing part of a multi-item request.

### Step 4 — Request Details (Conditional)

Fields shown depend on the request type:

| Request Type | Additional fields shown |
|---|---|
| `CoilDelivery` | Coil/material spec (pulled from work order's part if available), quantity |
| `DunnageDelivery` | Checklist (skids / Gaylords / boxes / other) — each creates a separate entry |
| `PartPickup` | Pickup location, quantity |
| `DieChange` | Notes (free text, optional) |
| `SetupAssistance` | Notes (free text, optional) |
| `QualityInspection` | Reason (first part / suspect issue / periodic check), can keep running checkbox |
| `MaintenanceRequest` | Description (required free text) |

### Step 5 — Review & Submit

Summary card showing:
- Workcenter selected
- Work order + sequence (if selected)
- Request type(s)
- Any additional details entered

"Submit" button calls `Service_WaitlistEntry.CreateEntryAsync()` → goes online to the API or queues offline. On success, navigates to the queue view showing the new entry.

---

## Favorites & Recents

Requested explicitly in the meeting:
> *"A favorites tab — things that they use the most. They can hit favorites, click it done."*

### Favorites

- A `Favorites` table (MySQL or local SQLite) stores a compressed snapshot of a completed wizard (workcenter + request type + any preset details)
- User manually stars a request after submitting it
- On the wizard landing screen, a "Favorites" row shows up to 5 starred requests as one-tap submitters
- Tapping a favorite pre-fills the entire wizard and jumps to Step 5 (Review), not Step 1

### Recents

- Last 5 submitted requests for the current user stored in `LocalDbContext` (offline-capable)
- Shown below Favorites on the wizard landing screen
- Tapping a recent pre-fills the wizard (same behavior as favorites)
- Retained for 7 days, then auto-purged by `SyncService`

---

## ViewModel Structure

```
ViewModels/
  Request/
    ViewModel_Waitlist_Request.cs    ← main wizard VM
```

Key observable properties:
```csharp
[ObservableProperty] ObservableCollection<Model_VisualWorkcenter> _availableWorkcenters
[ObservableProperty] Model_VisualWorkcenter? _selectedWorkcenter
[ObservableProperty] ObservableCollection<Model_VisualWorkOrder> _activeWorkOrders
[ObservableProperty] Model_VisualWorkOrder? _selectedWorkOrder
[ObservableProperty] ObservableCollection<Model_VisualWorkOrderSequence> _sequences
[ObservableProperty] Model_VisualWorkOrderSequence? _selectedSequence
[ObservableProperty] Enum_WaitlistRequestType _selectedRequestType
[ObservableProperty] ObservableCollection<WizardDunnageItem> _dunnageItems   // multi-select
[ObservableProperty] string _notes
[ObservableProperty] int _wizardStep   // 1–5
[ObservableProperty] bool _isVisualOffline
[ObservableProperty] bool _isBusy
[ObservableProperty] string _errorMessage
[ObservableProperty] ObservableCollection<Model_WaitlistEntry> _recentRequests
[ObservableProperty] ObservableCollection<Model_WaitlistFavorite> _favorites
```

Key commands:
```csharp
[RelayCommand] LoadWorkcentersAsync()
[RelayCommand] SelectWorkcenterAsync(Model_VisualWorkcenter wc)
[RelayCommand] LoadActiveWorkOrdersAsync()
[RelayCommand] SelectWorkOrderAsync(Model_VisualWorkOrder wo)
[RelayCommand] SelectSequenceAsync(Model_VisualWorkOrderSequence seq)
[RelayCommand] SelectRequestTypeAsync(Enum_WaitlistRequestType type)
[RelayCommand] SubmitRequestAsync()
[RelayCommand] SubmitFromFavoriteAsync(Model_WaitlistFavorite fav)
[RelayCommand] GoBackStepAsync()
```

---

## XAML Files to Create

```
Feature.Waitlist/
  Views/
    Request/
      View_Waitlist_Request.Windows.xaml
      View_Waitlist_Request.Android.xaml
      View_Waitlist_Request.xaml.cs
  ViewModels/
    Request/
      ViewModel_Waitlist_Request.cs
```

---

## New Core Models Needed

```
Core/Models/Waitlist/
  Model_WaitlistFavorite.cs    ← FavoriteId, UserId, WorkcenterId, RequestType, PresetNotes, UseCount, LastUsedAt
```

---

## Database: New Stored Procedure

`usp_Waitlist_CreateEntry` already planned in `Database/procedures/Waitlist/`. For the multi-entry dunnage case, the service calls this procedure once per checked item in a loop. No batch insert procedure needed — keep it simple.

---

## Windows vs Android Layout Notes

**Windows (press floor kiosk — large monitor):**
- Steps shown as a horizontal stepper at top
- Step content in center panel
- Workcenter and work order selection: two-column grid
- Request type: icon buttons in a 4-column grid
- Submit button: full-width, 48 px minimum height, high contrast

**Android (floor tablet / companion device):**
- Steps shown as a progress bar
- One section visible at a time (stack navigation or `CarouselView`)
- All tap targets: 64 px minimum
- Keyboard does not auto-open — all selections are tap/dropdown

---

## Open Decisions

- **Quality request routing:** When `QualityInspection` is selected, the meeting discussed whether this should go to a separate quality queue or the shared queue with a filter. Recommendation: single `WaitlistEntries` table, quality role sees entries filtered by `RequestType = QualityInspection`. Material handlers never see these.
- **Maintenance routing:** Should `MaintenanceRequest` go through this same waitlist or a separate system? The meeting did not resolve this. For v1, include it in the same queue and filter it from material handler views.
- **Dunnage item list:** The specific items in the dunnage checklist (skids, Gaylords, boxes, etc.) should be a configurable lookup in MySQL, not hardcoded. Stored in a `DunnageItems` or `RequestSubtypes` table — to be designed in a follow-on.
- **"Can keep running" flag:** For quality inspections, an operator indicates whether they can continue pressing while waiting. This should be a `bool` field on `WaitlistEntries` or in the `Notes` text for v1.
