# MTM Waitlist Application — Feature Index

**Last Updated:** May 10, 2026  
**Branch:** master  

---

## Overview

This index lists all planned features in priority and dependency order. Features are documented in individual files in `Documents/features/`. Each file covers background, architecture decisions, database objects, XAML files to create, and open decisions requiring stakeholder input.

---

## IMPORTANT

Upon the completion of ALL Features the AI Agent is responsible for validating that all Features have generated or updated any documentation files inside of .github/instructions, .github/copilot-instructions.md, AGENTS.MD, README.md (the core Readme file), as well as the creation of mds file we can later use when we get to the User Guide Phase of development.

---

## Implementation Order

```
FEATURE-01  Authentication & Login             ← Start here — blocks everything
    ↓
FEATURE-02  Infor Visual ERP Integration       ← Required for operator wizard
    ↓
FEATURE-03  Operator Request Wizard            ← Core MVP screen (operator-facing)
    ↓
FEATURE-04  Live Waitlist Queue View           ← Core MVP screen (all roles)
    ↓
FEATURE-05  Material Handler / Zone Mgmt       ← Handler claiming + zone routing
FEATURE-07  Setup Tech Module                  ← Press-to-WO assignment (parallel with 05)
    ↓
FEATURE-06  Lead Analytics & Dashboard         ← Analytics (replaces placeholder)
FEATURE-08  Quality Control Queue              ← Quality-filtered view (parallel with 06)
```

---

## Feature Summary

| # | Feature | Priority | Status | Depends On |
|---|---------|----------|--------|------------|
| [01](FEATURE-01-Authentication-Login.md) | Authentication & Login | Critical | In Progress | — |
| [02](FEATURE-02-InforVisual-ERP-Integration.md) | Infor Visual ERP Integration (Read-Only) | High | Ready to Design | 01 |
| [03](FEATURE-03-Operator-Request-Wizard.md) | Operator Request Wizard | High | Ready to Design | 01, 02 |
| [04](FEATURE-04-Live-Queue-View.md) | Live Waitlist Queue View | High | Ready to Design | 01, 03 |
| [05](FEATURE-05-Material-Handler-Zone-Task-Management.md) | Material Handler Zone & Task Mgmt | Medium-High | Design Required | 01, 04 |
| [06](FEATURE-06-Lead-Analytics-Dashboard.md) | Lead Analytics & Dashboard | Medium | Design Required | 01–05 |
| [07](FEATURE-07-Setup-Technician-Module.md) | Setup Technician Module | Medium | Design Required | 01, 02, 04 |
| [08](FEATURE-08-Quality-Control-Queue.md) | Quality Control Queue | Medium | Stakeholder Input Needed | 01, 04 |

---

## Architecture Notes Across All Features

### Infor Visual — "Sequence" not "Operation"

In the Infor Visual database, the individual steps on a work order are stored in the `OPERATION` table, but the identifying field is `SEQUENCE_NO` — this is what operators, leads, and setup techs call the "operation" or "step" when talking about a work order. In all UI labels, tooltips, and display text, use **"Sequence"** or **"Seq"** (e.g., "Seq 20"), not "Operation". Reserve the word "operation" for its plain-English meaning only.

### Read-Only ERP Rule

Infor Visual (`VISUAL\MTMFG`, SQL Server) is **read-only without exception**. All `Dao_InforVisualWorkOrder` connections use `ApplicationIntent=ReadOnly`. No stored procedures exist in Visual — only SELECT queries. All writes go to MySQL via the REST API.

### MySQL Stored Procedures

All MySQL writes go through stored procedures — no raw SQL INSERT/UPDATE/DELETE from application code. Procedure names follow `usp_<Domain>_<Action>` convention. See `Database/procedures/` for all existing procedures.

### v1 Scope Boundary

Per the original stakeholder meeting: v1 is feature parity with tablesready.com plus Infor Visual integration for work order lookup. All features labeled "future enhancement", "v2", or deferred in the individual docs are explicitly out of scope for v1 and must not be implemented until the base system is approved by Nick, Chris, and Dan.

---

## Outstanding Stakeholder Questions (All Features)

Questions that must be answered before implementation can proceed on specific features:

| Question | Blocking | Owner |
|---|---|---|
| Are workstation floor kiosks domain-joined? (affects `Integrated Security=True` for Visual) | FEATURE-02 | IT |
| What is the `SITE_ID` value for each plant location in `SHOP_RESOURCE`? | FEATURE-02 | Dan |
| Which `SHOP_RESOURCE` records are active press-floor workcenters? Is `SCHEDULE_NORMALLY = 'Y'` the right filter? | FEATURE-02 | Dan |
| What work order `STATUS` values (`O`, `R`, `H`) should be treated as "active" for the operator wizard? | FEATURE-02, 03 | Dan |
| Should quality notifications use email / Teams / sound-only for v1? | FEATURE-08 | Quality Lead, IT |
| Is forced auto-assignment (handler cannot skip a red task) desired, or just visual nudge? | FEATURE-05 | Todd, Doyle, Matt |
| Who can log a press into a job — setup techs only, or also supervisors? | FEATURE-07 | Dan, Nick |
| Should the quality inspection `Reason` list be configurable (MySQL lookup) or hardcoded for v1? | FEATURE-08 | Quality Lead |
| Should `MaintenanceRequest` go through this waitlist or a separate maintenance system? | FEATURE-03, 04 | Nick, Maintenance Lead |
