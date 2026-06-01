# Assumptions

1. Scope of the rewrite
   Why needed: The request says to go through the entire first setup workflow and the entire migration and danger zone workflows, scrap the current code, and start over.
   Impact if wrong: I could remove or rewrite code outside the intended recovery/setup surfaces.
   Alternative interpretations considered: Rewrite only the failing methods; rewrite the entire admin host.
   Working assumption: The rewrite scope is limited to the first-run wizard, migration page, danger-zone actions, and the services/helpers they directly depend on.

2. Preserve current UI shells
   Why needed: The request explicitly says not to remove UI elements.
   Impact if wrong: Rebuilding views or deleting controls would violate the request.
   Alternative interpretations considered: Replace the whole XAML pages with new layouts; keep only the same page names.
   Working assumption: Existing XAML pages and visible controls stay in place, while command bindings, state handling, and backing workflow logic can be replaced.

3. Desired workflow docs can be authored from current product intent and current failures
   Why needed: The request asks for desired workflow documentation before code removal, but no separate signed-off workflow spec was provided in this turn.
   Impact if wrong: The documentation could encode a workflow that differs from the intended operator experience.
   Alternative interpretations considered: Pause and ask for an external spec; derive desired behavior from current docs and issues.
   Working assumption: I should derive the desired workflows from the current first-run/migration UX, the recent failures, and the existing repo docs, then document them clearly before implementation.

4. Code removal means replacing workflow logic, not deleting shared app infrastructure
   Why needed: "Remove code" could mean deleting entire files, but the app still needs DI, navigation, models, and host plumbing to compile.
   Impact if wrong: I could over-delete infrastructure and create unrelated regressions.
   Alternative interpretations considered: Delete all related files entirely; only clear method bodies in touched workflow files.
   Working assumption: I should remove and replace the existing workflow logic inside the relevant services/viewmodels/helpers while preserving the surrounding file and DI structure needed by the app.

5. Validation must remain behavior-scoped
   Why needed: This rewrite affects setup/recovery flows that are currently unstable.
   Impact if wrong: Broad validation may miss first-run regressions or waste time on unrelated modules.
   Alternative interpretations considered: Validate only with a build; validate with narrow workflow tests plus build.
   Working assumption: I will add or update focused tests for the rewritten workflow logic and then run the server admin build.

Please review these assumptions. I am proceeding on them now because the request is explicit enough to continue, but if you want the rewrite scope narrowed or the desired workflows changed, say so before I finish the implementation.