# FEATURE-08: Quality Control Queue

**Status:** Design Required — Stakeholder Input Needed  
**Priority:** Medium  
**Depends On:** FEATURE-01, FEATURE-04 (queue infrastructure)  
**Blocks:** Nothing  

---

## Overview

Quality technicians need their own filtered view of the waitlist so they can respond to quality inspection requests without operators having to walk to the quality department. From the meeting:

> *"When an operator has bad parts, what I want to know is, how does quality know? Does the operator have to leave the press, go to quality, talk to somebody there and then go back? Why not just make a module in the wait list where when an operator adds a quality task, material handlers won't see it, but when somebody who is a quality technician opens the wait list, they'll see their wait list, and then all of a sudden be like, 14 has bad parts. Okay, go over there."*

---

## IMPORTANT

Upon the completion of this Feature the AI Agent is responsible for generating or updating any documentation files inside of .github/instructions, .github/copilot-instructions.md, AGENTS.MD, README.md (the core Readme file), as well as the creation of an md file we can later use when we get to the User Guide Phase of development.

---

## Confirmed Architecture Decision

Quality requests use the same `WaitlistEntries` table. Routing is by `RequestType = 'QualityInspection'`:
- Operators can submit `QualityInspection` requests via the standard wizard (FEATURE-03)
- Material handlers do **not** see `QualityInspection` entries in their queue filter
- Quality technicians see only `QualityInspection` entries in their queue
- Leads and supervisors see all entries including quality

This is already reflected in the current enum and database schema — no schema changes needed for the basic quality queue.

---

## Quality-Specific Request Details

When an operator submits a `QualityInspection` request, FEATURE-03's conditional fields include:

| Field | Type | Required | Notes |
|---|---|---|---|
| Reason | Enum | Yes | `FirstPart`, `SuspectIssue`, `PeriodicCheck`, `Rework`, `CustomerReturn` |
| Can Keep Running | bool | Yes | Can operator continue pressing while waiting for quality? |
| Notes | string | No | Free text for specific details (e.g., "Dimension out on the flange hole") |

These map to `WaitlistEntries.Notes` (JSON-encoded details) for v1, or dedicated columns if volume justifies it.

---

## Quality Technician Dashboard

Simple filtered queue — same layout as material handler queue but filtered to `RequestType = 'QualityInspection'`:

```
┌───────────────────────────────────────────────────────┐
│ QUALITY QUEUE                               [≡]       │
├───────────────────────────────────────────────────────┤
│ PRESS-14 · First Part Inspection · 8 min              │
│ WO-123456 · Seq 20 · Operator: CAN KEEP RUNNING       │
│ [GO] [COMPLETE]                                       │
├───────────────────────────────────────────────────────┤
│ PRESS-22 · Suspect Issue · 3 min 🟡                   │
│ Note: "Hole dimension looks off"                      │
│ [GO] [COMPLETE]                                       │
└───────────────────────────────────────────────────────┘
```

"Can Keep Running" is prominently displayed so quality techs know if the press is idle waiting for them.

---

## Notification Discussion (Unresolved)

The meeting had significant discussion about how to alert quality techs who may not be watching the screen:

| Option | Status | Notes |
|---|---|---|
| App queue (polling) | ✅ Implementable v1 | Requires quality tech to have the app open |
| Email alert | ⚠️ Security approval needed | IT/security must approve outbound SMTP |
| Teams message | ⚠️ Approval needed | Teams webhook requires admin approval |
| Intercom / PA announcement | ❌ Ruled out | Office noise concerns |
| Phone push notification | ❌ Ruled out | Phone apps not allowed (per Nick/Chris) |

> *"It's more of talking with quality and getting a game plan with quality, and what's going to work best with quality."*

**Recommendation for v1:** App queue only. An audible alert (Windows system sound) when a new quality request comes in — the app uses `MediaElement` or `SystemSounds` to play a brief alert tone. This requires the quality station to have the app open and the volume on, which is a staffing/process decision, not a technical one.

Email and Teams integration are deferred to v2 pending IT approval. The notification framework from the original kickoff document (`kickoff-revised-core-first.md`) can be stubbed in now and activated later.

---

## Quality Request Reasons Lookup

A MySQL table for configurable quality inspection reasons (so new reasons can be added without code changes):

```sql
CREATE TABLE QualityInspectionReasons (
    Id          INT UNSIGNED NOT NULL AUTO_INCREMENT,
    ReasonCode  VARCHAR(50) NOT NULL,
    DisplayName VARCHAR(100) NOT NULL,
    IsActive    TINYINT(1) NOT NULL DEFAULT 1,
    SortOrder   SMALLINT NOT NULL DEFAULT 0,
    CreatedAt   DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    UpdatedAt   DATETIME NOT NULL DEFAULT UTC_TIMESTAMP(),
    PRIMARY KEY pk_QualityInspectionReasons (Id),
    UNIQUE uq_QualityInspectionReasons_Code (ReasonCode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Seed data
INSERT INTO QualityInspectionReasons (ReasonCode, DisplayName, SortOrder) VALUES
('FirstPart',       'First Part Approval',    1),
('SuspectIssue',    'Suspect Quality Issue',  2),
('PeriodicCheck',   'Periodic Check',         3),
('Rework',          'Rework Needed',          4),
('CustomerReturn',  'Customer Return',        5);
```

---

## XAML Files

```
Feature.Waitlist/
  Views/
    Quality/
      View_Waitlist_Quality.Windows.xaml
      View_Waitlist_Quality.Android.xaml
      View_Waitlist_Quality.xaml.cs
  ViewModels/
    Quality/
      ViewModel_Waitlist_Quality.cs
```

This view is largely the same as FEATURE-04's queue view, filtered to quality requests only. The ViewModel can inherit or compose `ViewModel_Waitlist_Queue` rather than duplicating logic.

---

## Open Decisions

- **Who needs to be consulted:** The meeting specifically noted that quality leadership (new person) needs to be involved in decisions about their workflow. Do not finalize the quality notification approach until that conversation happens.
- **Quality request priority vs. other requests:** Should `QualityInspection` requests automatically have higher priority than, say, a `DunnageDelivery`? The meeting discussion implied urgency but did not set an explicit rule. Recommendation: quality requests default to `High` priority (vs. `Normal` for most others) — visible to the lead but not forced-first in the queue.
- **Spot welding / rework queue:** The meeting mentioned a specific Spot Welding / John Deere Doors rework situation. If there are location-specific quality workflows, they may need sub-filtering within the quality queue. Defer to stakeholder input.
- **Quality completing vs. closing:** When a quality tech "completes" a quality request in the app, does that mean the issue is resolved, or just that they visited the press? For v1, completing = visited. Resolution tracking is a future enhancement.
