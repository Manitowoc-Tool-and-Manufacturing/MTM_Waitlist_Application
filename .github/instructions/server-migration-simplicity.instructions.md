---
applyTo: "MTM_Waitlist_Server/**/*.{cs,sql,md}"
description: "Server admin migration, first-run, and database drift handling rules"
---

# Server Migration Simplicity Rules

## Core Policy

1. Keep versioned migration SQL files to a minimum.
2. Treat checked-in SQL object files as the source of truth for live tables, procedures, triggers, and indexes.
3. Prefer safe, data-preserving alters over destructive rebuilds.
4. Do not introduce new broad replay or rerun-all behavior when a compare-and-update flow can solve the problem.

## Schema Change Rules

1. Schema changes must be designed to avoid data loss.
2. Prefer additive changes first: add columns nullable or with safe defaults, backfill, then tighten constraints only when safe.
3. Avoid dropping populated columns or tables in normal update paths.
4. Do not drop indexes that may be supporting foreign key constraints as part of routine rerun logic.
5. Toggle foreign key checks only for tightly controlled, reversible operations where it is required and justified.
6. Defaults, nullability, indexes, and constraints should be authored so the live schema can be updated in place.

## Migration Page Rules

1. The migration page should compare the stored SQL file to the live object it defines.
2. If the stored SQL and live object match, do not show the object as pending.
3. If the live object is missing or drifted, add it to the page as needing update.
4. Pending work should be object-based and drift-based, not only version-file-based.
5. Preview should help the operator understand what object will be updated and why it was flagged.

## SQL Comparison Rules

1. For tables, compare normalized metadata signatures built from information_schema.
2. For procedures and triggers, compare normalized `SHOW CREATE` output to normalized stored SQL.
3. For indexes, compare live index definitions to stored SQL signatures.
4. Normalization should ignore irrelevant formatting differences and focus on meaning.

## First-Run And Danger Zone Rules

1. First-run bootstrap must be resumable after partial success.
2. Danger-zone reset must explicitly return the app to a clean first-run state.
3. Do not let stale state silently advance first-run steps after a destructive reset.

## Implementation Bias

1. Prefer a small number of clear workflow services over accumulating patch logic in many places.
2. Prefer deterministic comparison/update steps over heuristics and retry chains.
3. Keep existing UI elements when requested, but replace backing workflow logic freely when the current design is unstable.