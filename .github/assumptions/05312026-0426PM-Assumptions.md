# FEATURE-07 Assumptions

Please review these assumptions before the implementation moves into the cross-database dunnage and InforVisual integration slices.

## 1. MTM Receiving App source is available outside the current workspace

Why this assumption is needed:
The active workspace does not include the MTM Receiving Application as a workspace folder, but you provided its repository path separately: `C:\Users\johnk\source\repos\MTM_Receiving_Application`.

Potential impact if wrong:
If the external receiving-app repo at that path is not the production source of truth, the dunnage catalog endpoint and SetupTech dunnage UX could still be implemented against the wrong contract.

Alternative interpretations considered:
- The provided receiving-app repo path is the correct implementation source for dunnage logic.
- The feature docs remain the intended contract and the external repo is only a reference.

Assumption:
For receiving-app-dependent work, I can now consult the external repo at `C:\Users\johnk\source\repos\MTM_Receiving_Application` and reconcile it with the documented `mtm_receiving_application.dunnage_parts`, `dunnage_types`, and related Feature 7 notes.

## 2. Feature 7 implementation should start with workspace-resident foundation slices

Why this assumption is needed:
The feature docs span SQL schema, server APIs, client contracts, services, viewmodels, and platform views. Implementing all of it safely requires an order.

Potential impact if wrong:
If you expected the first pass to begin with UI mockup conversion or with the cross-database dunnage endpoint before schema/contracts, my implementation order would not match your preferred sequencing.

Alternative interpretations considered:
- Start from database schema and contracts first.
- Start from UI screens first using mockups and stub data.
- Start from server-side InforVisual and dunnage endpoints first.

Assumption:
I will continue in this order unless you redirect it: server pre-flight blockers, Feature 7 database/schema artifacts, shared client/server contracts, then Feature 7 server/client implementation slices.

## 3. InforVisual behavior will be implemented from the Feature 2 and Feature 7 docs plus the curated Infor docs already in this repo

Why this assumption is needed:
The feature request explicitly says to review all related Infor docs, and those are present here, but a live receiving-app query implementation is not.

Potential impact if wrong:
If there are newer production query rules or connection details outside this repo, the first-pass InforVisual query layer may need follow-up adjustment.

Alternative interpretations considered:
- Wait for an external query source before starting.
- Use only the feature docs.
- Use the feature docs plus curated Infor reference docs in this repo.

Assumption:
I will treat the Feature 2 and Feature 7 documents plus the curated `Documents/InforVisualRelated/` references as the implementation contract for the initial pass.

## Confirmation Requested

If any of these assumptions are wrong, tell me before I reach the dunnage-catalog and InforVisual endpoint slices. Otherwise I will continue implementing the workspace-resident Feature 7 foundation next.
