# Assumptions — Startup Lifecycle Services

1. The standalone WinUI host owns Windows-specific startup lifecycle behavior.
   - Why needed: The Windows host was converted away from MAUI startup, so the existing shared MAUI `App` kill-switch startup logic no longer runs for WinUI.
   - Potential impact if wrong: Duplicating startup behavior in the WinUI host could conflict with shared MAUI startup if Windows is later moved back into the shared host.
   - Alternative considered: Put all startup lifecycle logic in the shared MAUI project. This does not fit the current standalone WinUI host boundary.

2. The Droid lifecycle service should live in the shared MAUI project and be registered only for Android.
   - Why needed: The Droid host is intentionally thin and references the shared MAUI project only.
   - Potential impact if wrong: Placing Android lifecycle logic directly in the Droid host would violate the thin-launcher convention.
   - Alternative considered: Add Android lifecycle logic to `MainActivity`. This was rejected because app startup business behavior belongs in services.

3. Kill-switch heartbeat should start only after a valid authenticated session exists.
   - Why needed: The heartbeat endpoint is authorized and the service needs username/display-name identity.
   - Potential impact if wrong: Starting before authentication would generate unauthorized requests and would not show the client in the admin app.
   - Alternative considered: Anonymous startup heartbeat. This would require server contract changes and weaker tracking.

4. For WinUI, startup lifecycle should be invoked from `MainWindow` after login or stored-session validation.
   - Why needed: `MainWindow` owns the current login-to-shell transition in the standalone WinUI host.
   - Potential impact if wrong: Starting lifecycle too early could miss the authenticated identity or run before WinUI UI objects are initialized.
   - Alternative considered: Invoke from `App.OnLaunched`. This is too early for authenticated-only lifecycle services.

5. The immediate fix should preserve existing auth ViewModel events rather than changing the auth service contract.
   - Why needed: The auth ViewModel is used by both WinUI and MAUI pages, and changing its event payload would ripple into tests and feature code.
   - Potential impact if wrong: The lifecycle service must read identity from SecureStorage after authentication rather than receiving the token directly.
   - Alternative considered: Add token payload to the authenticated event. This is cleaner long term but a larger cross-feature change.

Please confirm if any of these assumptions are incorrect before the lifecycle design is expanded further.