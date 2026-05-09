# Android Startup Workflow and Edge Case Review

## Purpose

This document explains what happens when the Android version of the MTM Waitlist app starts, what alternate paths may happen during startup, and what edge cases could affect the user experience.

This is written for a non-developer audience. It avoids code-level detail and focuses on behavior, risk, and possible fixes.

---

## Short Summary

The Android app has a fairly simple visible startup path:

1. Android launches the app.
2. The app loads shared settings, fonts, styles, and services.
3. The app starts a background sync listener for network changes.
4. In Debug builds, it checks whether the main API server is reachable.
5. The app opens the main dashboard screen.

The most important startup risks are:

- Network permission or network-security settings can stop network checks from working.
- The sync service starts very early, so problems in connectivity or local storage can happen before the first screen appears.
- The local database is used before it is clearly initialized at startup.
- Debug-only mock data seeding runs in the background and may hide real startup/data problems during development.
- Android-specific XAML selection must package the Android dashboard layout correctly.

---

## Startup Workflow

## 1. Android launches the app

Android starts the application process and creates the app's Android application object.

### Normal path

- Android starts the app from the launcher icon.
- The app uses the Android splash theme.
- The app creates the MAUI application.

### Branches and events

- **Cold start:** The app was not running. Everything is created from scratch.
- **Warm start:** Android may bring an existing app process or activity back to the front.
- **Configuration change:** Screen rotation, theme changes, density changes, or screen-size changes are handled by the activity without recreating the whole app.
- **Single-top launch:** If the user taps the launcher while the app is already open, Android should reuse the existing main activity instead of stacking duplicate launch screens.

### Edge cases

| Situation | User impact | Possible fix |
|---|---|---|
| Android kills the app in the background and later restarts it | The app may appear to restart unexpectedly | Make startup safe to run repeatedly |
| Startup work takes too long | User may see the splash screen longer than expected | Keep heavy work off the startup path |
| App is opened again while already running | User should return to the existing app, not a duplicate screen | Keep `SingleTop` launch behavior |

---

## 2. Android-specific configuration runs

The Android host creates the MAUI app builder, then hands setup over to the shared app startup code.

### Normal path

- Android creates the app builder.
- No extra Android-only startup logic currently runs.
- Shared startup setup is called.

### Branches and events

- Future Android-only features, such as push notifications, camera access, GPS, biometrics, or Android-specific storage setup, would be added before shared startup runs.

### Edge cases

| Situation | User impact | Possible fix |
|---|---|---|
| Android-only setup is added later and fails before shared startup | The app could crash before the first screen | Keep Android-only setup small and fail-safe |
| Permissions for future Android-only features are missing | Feature may fail or crash when first used | Add manifest permissions and runtime permission checks where required |
| Business logic is added to Android startup | Startup becomes harder to diagnose and less consistent with Windows | Keep Android startup as a thin launcher only |

---

## 3. Shared app setup begins

The shared startup code configures the MAUI app, fonts, logging, settings, and dependency registrations.

### Normal path

- The app registers the main application class.
- Fonts are registered.
- Debug logging is enabled in Debug builds.
- App settings are loaded.
- Services and repositories are registered.

### Branches and events

- **Debug build:** Debug logging is enabled and development settings may also load.
- **Release build:** Debug logging and development-only settings are not loaded.

### Edge cases

| Situation | User impact | Possible fix |
|---|---|---|
| `appsettings.json` is missing from the packaged app | Startup can fail before the first screen | Keep appsettings embedded and verify packaging during build |
| Development settings are missing in Debug | The app falls back to normal settings | Acceptable if intentional; otherwise verify Debug resource packaging |
| Font files are missing or misnamed | Text may render with fallback fonts or look wrong | Verify font resources are present and packaged |
| A dependency is not registered | App can crash when a screen or service is created | Validate dependency registration before release |

---

## 4. Network permissions and security rules are applied

The Android manifest declares what the app is allowed to do. The network security file controls which HTTP connections are allowed.

### Normal path

- The app has internet permission.
- The app has network-state permission.
- Cleartext HTTP is allowed only for the internal work-network server.
- Other cleartext HTTP traffic is blocked.

### Branches and events

- **Work-network server:** HTTP traffic to `172.16.1.104` is allowed.
- **Local developer API:** The app may try to use a fallback local API address.
- **Other HTTP hosts:** Android should block cleartext traffic.
- **HTTPS traffic:** Allowed using system certificates.

### Edge cases

| Situation | User impact | Possible fix |
|---|---|---|
| `ACCESS_NETWORK_STATE` is missing | The app can crash during startup when connectivity is checked | Keep `android.permission.ACCESS_NETWORK_STATE` in the manifest |
| `INTERNET` is missing | API calls fail | Keep `android.permission.INTERNET` in the manifest |
| Fallback API uses HTTP on Android emulator | It may be blocked unless allowed by network security rules | Add explicit debug-only allowance if local HTTP fallback is required |
| Work server IP changes | App cannot reach the API | Update app settings and network security rules together |
| Release app points to local development API | Users cannot connect | Use release-safe configuration before publishing |

---

## 5. Services and data access are registered

The app registers shared infrastructure, local storage, API access, business services, pages, and view models.

### Normal path

- Connectivity checking is registered.
- Local database access is registered.
- API access is registered.
- Waitlist services are registered.
- Dashboard page and dashboard view model are registered.

### Branches and events

- A service may not be created immediately. It is usually created when first needed.
- The sync service is an exception: it is created immediately during startup.

### Edge cases

| Situation | User impact | Possible fix |
|---|---|---|
| A required service is not registered | The app may crash when opening a page or starting sync | Add a startup validation check or test coverage |
| A page is registered with the wrong lifetime | Screen state may leak or behave unexpectedly | Keep pages and view models transient |
| Shared startup registers too many services too early | Startup can become slow or fragile | Only eagerly start services that must run before the first screen |

---

## 6. Sync service starts early

The sync service is created during startup so it can listen for network changes before the user opens a screen.

### Normal path

- Sync service starts.
- It subscribes to network connectivity changes.
- When the device later gains internet access, it tries to send pending offline changes to the API.

### Branches and events

- **No network change happens:** The sync listener waits silently.
- **Internet becomes available:** Pending offline changes are sent.
- **Internet is lost:** No sync is attempted.
- **The API is unreachable even though Android reports internet access:** Sync attempts can fail and pending changes remain queued.

### Edge cases

| Situation | User impact | Possible fix |
|---|---|---|
| Connectivity permission is missing | Startup crash | Already fixed by adding `ACCESS_NETWORK_STATE` |
| Connectivity changes rapidly | Multiple sync attempts may overlap | Add a sync-in-progress guard |
| Sync fails during the event handler | The comment says errors are swallowed, but the current flow does not clearly catch all errors in the event handler | Wrap sync event handling in safe error handling |
| Local database tables are not ready when sync runs | Sync may fail to read the pending queue | Initialize local database before starting sync |
| Pending queue contains an unknown operation | That queue item remains unresolved | Log or surface failed queue items for review |
| Pending queue has one bad item | Other items may still sync, but the bad item may remain forever | Add retry limits or a failed-sync status |
| User has internet but not access to the work server | App may behave as online, then silently fall back | Show clearer connection state to the user |

---

## 7. Debug-only API reachability check runs

In Debug builds, the app starts a background task that checks whether the primary API server is reachable.

### Normal path

- The app checks the primary API health endpoint.
- If the primary API is not reachable, it seeds local mock data if the local database is empty.
- This happens in the background so the first screen can load without waiting.

### Branches and events

- **Primary API reachable:** No mock data is seeded.
- **Primary API unreachable:** Local mock data may be added.
- **Local database already has data:** No mock data is added.
- **Release build:** This check does not run.

### Edge cases

| Situation | User impact | Possible fix |
|---|---|---|
| Background task fails unexpectedly | Failure may be hard to see because it is fire-and-forget | Add safe logging around the background task |
| Local database has not been initialized | Mock seeding may fail or silently do nothing | Initialize local database before seeding |
| Primary API is slow but available | The app may seed mock data even though the server eventually responds | Tune timeout or make seeding manually triggered |
| Mock data appears during development | Developers may think real API data loaded | Clearly label mock data in the UI during Debug |
| Debug behavior differs from Release | A bug may only appear after publishing | Test Android Release startup before deployment |

---

## 8. Local database is used

The app has a local SQLite database for offline cache and pending offline writes.

### Normal path

- The local database connection is created when first used.
- Tables are expected to exist before repositories read or write data.

### Branches and events

- The database may first be used by sync.
- The database may first be used by mock data seeding.
- The database may first be used by a future screen that loads waitlist data.

### Edge cases

| Situation | User impact | Possible fix |
|---|---|---|
| Database tables are not created before first use | Offline reads, writes, seeding, or sync can fail | Call database initialization once during startup before sync/seeding |
| Database file is corrupted | Offline data may fail to load | Add recovery guidance or recreate flow |
| App is updated and database schema changes | Older installs may fail or lose expected fields | Add database migration handling |
| Multiple background operations use the database at startup | Race conditions or timing-dependent failures may happen | Initialize once before background work starts |

---

## 9. The main app window is created

After startup setup finishes, the app creates the main window and loads the application shell.

### Normal path

- App resources load.
- The app creates a window.
- The shell is placed inside the window.
- The dashboard is the first visible area.

### Branches and events

- **Light theme:** Light resources are applied.
- **Dark theme:** Dark resources are applied.
- **Android flyout navigation:** The navigation menu is available as a flyout.

### Edge cases

| Situation | User impact | Possible fix |
|---|---|---|
| A style resource is missing or invalid | The app may fail when loading the first window | Validate XAML resources during build and startup testing |
| Android dark/light theme changes while running | Colors and resources may change | Test theme switching on Android |
| Shell route or first screen fails to load | Blank screen or startup crash | Validate first screen construction and XAML packaging |

---

## 10. Dashboard screen loads

The shell points to the Dashboard page. The page receives its view model through dependency injection.

### Normal path

- Dashboard page is created.
- Dashboard view model is created.
- Android dashboard layout is loaded.
- The screen shows placeholder dashboard content and status text.

### Branches and events

- Android should load the Android-specific dashboard XAML.
- Windows should load the Windows-specific dashboard XAML.
- Future feature pages may be added to the shell.

### Edge cases

| Situation | User impact | Possible fix |
|---|---|---|
| Android-specific XAML is not packaged | Dashboard may fail to load on Android | Keep platform-specific XAML item rules tested |
| A view model dependency is added later but not registered | Dashboard may crash when opened | Update service registration with each new page dependency |
| Dashboard starts loading real data later | Network or database issues may appear on first screen load | Add loading, empty, offline, and error states |
| The Android layout grows too dense | Poor mobile usability | Keep Android screens single-task and mobile-sized |

---

## Event-Based Branches After Startup

## Network changes

When Android reports that internet access is available, the sync service tries to flush pending offline writes.

### Edge cases

- Android may report internet access even if the work API cannot be reached.
- The user may be on public Wi-Fi with internet but no route to the internal work server.
- The API may reject a request because the auth token is missing or expired.
- Multiple network changes can happen quickly.

### Suggested improvements

- Show a user-friendly connection state, such as `Offline`, `Internet available`, or `Work server reachable`.
- Prevent overlapping sync runs.
- Record sync failures in a visible or diagnosable way.

---

## App returns from background

Android may pause the app, keep it in memory, or kill it. When the user returns, startup may or may not run again.

### Edge cases

- Sync listener may already exist and should not duplicate itself.
- Stale data may remain visible after a long background period.
- Network state may have changed while the app was backgrounded.

### Suggested improvements

- Refresh important data when the app resumes.
- Make sync startup idempotent, meaning it is safe if run more than once.

---

## Device configuration changes

Android may notify the activity when screen size, density, orientation, or theme changes.

### Edge cases

- The dashboard may be usable in portrait but cramped in landscape or split-screen.
- Dark mode colors may have poor contrast.
- Font scaling may cause clipped text.

### Suggested improvements

- Test Android with large font sizes.
- Test light mode and dark mode.
- Test portrait, landscape, and split-screen where available.

---

## Highest-Priority Risks

## 1. Local database initialization is not clearly part of startup

The local database has an initialization method, but startup currently relies on services/repositories using the database when needed. Sync and mock seeding can use the database very early.

### Why this matters

If database tables do not exist yet, offline sync or mock data seeding may fail before the user sees useful information.

### Possible fix

Initialize the local database once before starting sync and before running debug mock seeding.

---

## 2. Sync can be triggered by fast or repeated network changes

The sync service reacts to Android connectivity events. Mobile devices can rapidly switch between Wi-Fi, mobile data, captive portals, and disconnected states.

### Why this matters

Multiple sync attempts may overlap, causing duplicate API calls or timing issues.

### Possible fix

Add a simple `sync already running` guard and safe error logging.

---

## 3. Internet access does not guarantee access to the work API

Android's `Internet` status only means the device has some internet access. It does not prove the internal work server is reachable.

### Why this matters

The app may think it is online but still fail to reach the MTM API.

### Possible fix

Track the work API reachability separately from general internet connectivity.

---

## 4. Debug startup behavior can hide Release behavior

Debug builds can seed mock data in the background. Release builds do not.

### Why this matters

A developer may see data in Debug and assume startup works, while Release shows an empty or different state.

### Possible fix

Test Android Release startup regularly and clearly mark mock data in Debug.

---

## 5. Local HTTP fallback may be blocked by Android network security

The app rewrites `localhost` to Android emulator host address `10.0.2.2`, but network security only explicitly allows cleartext HTTP to `172.16.1.104`.

### Why this matters

The fallback development API may not work on Android if cleartext HTTP to `10.0.2.2` is blocked.

### Possible fix

If local Android emulator API testing is required, add a debug-only network security allowance for the emulator host.

---

## Suggested Fix List

## Recommended now

1. Initialize the local database before starting sync or mock seeding. **Implemented.**
2. Add safe error handling around the sync connectivity event. **Implemented.**
3. Prevent overlapping sync attempts. **Implemented.**
4. Verify Android Release startup after the manifest permission change. **Build verified; device startup testing still recommended.**
5. Decide whether Android emulator HTTP fallback should be supported and configure network security accordingly. **Implemented for `10.0.2.2`.**

## Recommended before adding more screens

1. Add visible offline and API-unreachable states.
2. Add a startup health check for required services.
3. Add a first-screen loading/error/empty state pattern.
4. Add clear diagnostics for sync failures.
5. Add tests or build checks for Android-specific XAML packaging.

---

## Current Status

The previously observed startup crash was caused by a missing Android network-state permission. That permission has now been added.

The local database is now initialized during shared startup before the sync service is resolved and before Debug mock seeding can run. Sync connectivity handling now catches event-handler failures and prevents overlapping queue flushes. Android emulator HTTP fallback to `10.0.2.2` is now explicitly allowed by network security configuration.

Android Release startup should still be tested on a real device or emulator before deployment because build verification does not prove runtime network reachability, packaged resource behavior, or API availability.
