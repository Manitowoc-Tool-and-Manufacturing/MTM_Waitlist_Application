# FEATURE-01: Authentication & Login

**Status:** Core Flow Implemented  
**Priority:** Critical — Blocks Everything Else  
**Depends On:** Running server admin app / API host (`MTM_Waitlist_Server`)  
**Blocks:** All other features  

---

## Overview

The server and database foundations now exist in the separate `MTM_Waitlist_Server/` solution, and the core auth API surface is now implemented there: `login`, `refresh`, `revoke`, `check-workstation`, `auto-login`, and `/health`.

On the client side (WinUI 3 for Windows, .NET MAUI for Android), `Feature.Auth` now provides the initial login experience, secure token persistence, stored-session startup bypass, shared-workstation detection, and silent Windows auto-login on personal workstations.

---

## IMPORTANT

Upon the completion of this Feature the AI Agent is responsible for generating or updating any documentation files inside of .github/instructions, .github/copilot-instructions.md, AGENTS.MD, README.md (the core Readme file), as well as the creation of an md file we can later use when we get to the User Guide Phase of development.

---

## Background: Confirmed Auth Model

Two login modes were confirmed during schema finalization (May 10, 2026):

| Workstation type | Behavior |
|---|---|
| **Personal workstation** | `WindowsUsername` stored in `Users.WindowsUsername`. App calls `/api/auth/check-workstation` → if not shared, calls `/api/auth/auto-login` with the Windows username → silent login, no credential prompt. |
| **Shared kiosk / floor terminal** | `WindowsUsername` stored in `SharedWorkstations`. App calls `/api/auth/check-workstation` → returns `IsShared = true` → shows badge + PIN credential screen. |

---

## What to Build

### 1. `View_Auth_Login` (`Feature.Auth`)

**Windows layout:**
- Full-screen centered card — not a navigation flyout item
- Two states driven by workstation mode:
  - **Auto-login in progress:** spinner while the app checks the workstation and attempts silent sign-in
  - **Credential entry:** username field + password/PIN entry for shared workstations or fallback failures
- Error banner for failed logins (`InvalidCredentials`, `AccountLocked`, network failure)
- No "Remember Me" — JWT refresh handles session continuity

**Android layout:**
- Full-screen username/password login with shared-workstation fallback
- Custom numeric PIN pad remains a future UX enhancement

### 2. `ViewModel_Auth_Login`

```
[ObservableProperty] bool _isCheckingWorkstation   // spinner during workstation check / auto-login
[ObservableProperty] bool _isSharedWorkstation
[ObservableProperty] bool _isAuthenticating        // spinner during login call
[ObservableProperty] string _username
[ObservableProperty] string _password
[ObservableProperty] string _errorMessage

[RelayCommand] InitializeAsync()   // startup workstation check and Windows auto-login
[RelayCommand] LoginAsync()        // credential submit
[RelayCommand] ClearPasswordAsync()     // reset password field
```

**Current startup flow:**
```
App.CreateWindow()
  → if a stored token exists and is not expired:
      navigate to the main shell (WinUI 3 `NavigationView` on Windows / MAUI `AppShell` on Android)
  → else:
      open View_Auth_Login

InitializeAsync()
  → resolve current Windows identity on Windows
  → Service_Auth.CheckWorkstationAsync(windowsUsername)
  → if IsSharedWorkstation == false:
      Service_Auth.AutoLoginAsync(windowsUsername)
      → success → navigate to main shell
      → failure → fall back to credential screen
  → if IsSharedWorkstation == true:
      show credential screen

LoginAsync()
  → Service_Auth.LoginAsync(username, password)
  → success → navigate to main shell
  → failure → show error banner
```

### 3. Session lifecycle

- On successful login, `Service_Auth` stores JWT via `SecureStorage` — already implemented
- `Service_Auth` also stores the refresh token, expiry timestamp, and current role
- App startup checks the stored expiry and bypasses the login page when the session is still valid
- Token refresh is handled by `Service_Auth.RefreshTokenAsync()` using the persisted refresh token
- On token expiry or 401, navigate back to login screen
- Session timeout: not auto-logout for personal workstations; shared kiosks → configurable idle timeout (future enhancement, stubbed as a no-op for v1)

### 4. Role-based navigation after login

After successful auth, the role is persisted from the auth response. The current implementation reads and preserves role information, but all roles still land on `Dashboard` because that is the only authenticated feature page implemented today.

Target landing pages once the remaining feature pages exist:

| Role | Landing page |
|---|---|
| `PressOperation` | Waitlist request queue (operator view) |
| `SetupTech` | Waitlist queue (setup view) |
| `MaterialHandler` | Material handler task queue |
| `ProductionSupervisor` / `ProductionManager` | Dashboard (analytics) |
| `Quality` | Quality queue |
| `Admin` / `Developer` | Dashboard |
| `Receiving` | Dashboard |

---

## Infor Visual Integration

None required for this feature. Auth is entirely MySQL-backed.

---

## Database Objects Needed

All exist in `Database/procedures/Auth/`:
- `usp_Auth_CheckSharedWorkstation` — accepts `MachineName`
- `usp_Auth_GetUserByWindowsUsername` — accepts `WindowsUsername`
- `usp_Auth_ValidateCredentials` — accepts `Username` + bcrypt hash

---

## Files to Create

| File | Project |
|---|---|
| `Views/Login/View_Auth_Login.Windows.xaml` | Feature.Auth |
| `Views/Login/View_Auth_Login.Android.xaml` | Feature.Auth |
| `Views/Login/View_Auth_Login.xaml.cs` | Feature.Auth |
| `ViewModels/Login/ViewModel_Auth_Login.cs` | Feature.Auth |

---

## Open Decisions

- **Project placement:** Resolved — auth lives in `Feature.Auth`.
- **PIN vs password:** The original meeting used "badge + PIN". Current schema stores a `bcrypt` password hash. The UI should present a numeric PIN pad. The PIN is the user's password stored hashed — no separate PIN field needed in the schema.
- **Idle timeout for kiosks:** Stubbed out as no-op in v1. A future enhancement would monitor `PointerMoved`/`Tapped` events and auto-logout shared workstations after N minutes.
- **Role-specific destinations:** Auth now preserves role information, but non-dashboard landing pages still depend on the corresponding features being built.
