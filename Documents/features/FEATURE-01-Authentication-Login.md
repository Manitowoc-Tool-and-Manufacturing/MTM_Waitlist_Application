# FEATURE-01: Authentication & Login

**Status:** In Progress  
**Priority:** Critical — Blocks Everything Else  
**Depends On:** `MTM_Waitlist_Server` auth controller completion for workstation detection / auto-login  
**Blocks:** All other features  

---

## Overview

The server and database foundations now exist in the separate `MTM_Waitlist_Server/` solution. On the MAUI client side, `Service_Auth`, `IService_Auth`, and secure JWT storage are already present. The database tables and procedures for auth also exist in the server solution.

What is **not** fully implemented yet is the complete auth API surface described in the original design. The current server `AuthController` exposes stubbed `login`, `refresh`, and `revoke` endpoints, while `auto-login` and `check-workstation` are not yet implemented there. This feature therefore starts with the login UI in `Feature.Auth` and will layer in workstation detection once the server endpoints are completed.

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
- Initial implementation: credential entry screen using username + password/PIN
- Future workstation-aware states (pending server endpoints):
  - **Auto-login in progress:** spinner + "Signing in as [Windows username]…"
  - **Credential entry:** username field + PIN pad (large tap targets, 48 px minimum)
- Error banner for failed logins (`InvalidCredentials`, `AccountLocked`, network failure)
- No "Remember Me" — JWT refresh handles session continuity

**Android layout:**
- Initial implementation: full-screen username/password login
- Future kiosk-specific PIN pad remains planned once workstation detection is available

### 2. `ViewModel_Auth_Login`

```
[ObservableProperty] bool _isAuthenticating        // spinner during login call
[ObservableProperty] string _username
[ObservableProperty] string _password
[ObservableProperty] string _errorMessage

[RelayCommand] InitializeAsync()   // called OnAppearing — pre-fills username and clears stale errors
[RelayCommand] LoginAsync()        // credential submit
[RelayCommand] ClearPasswordAsync()     // reset password field
```

**Current startup flow (implemented first):**
```
InitializeAsync()
  → prefill Username from current environment when available
  → show credential screen
LoginAsync()
  → Service_Auth.LoginAsync(username, password)
  → success → navigate to AppShell
  → failure → show error banner
```

**Future startup flow (pending server implementation):**
```
InitializeAsync()
  → Service_Auth.CheckWorkstationAsync(Environment.MachineName)
  → if IsShared == false:
      Service_Auth.AutoLoginAsync(WindowsIdentity.GetCurrent().Name)
      → success → navigate to AppShell
      → failure → show credential screen (fallback)
  → if IsShared == true:
      IsSharedWorkstation = true → show credential screen
```

### 3. Session lifecycle

- On successful login, `Service_Auth` stores JWT via `SecureStorage` — already implemented
- Initial implementation starts on the login page and navigates to `AppShell` after successful login
- Token refresh is handled by `Service_Auth.RefreshTokenAsync()`
- On token expiry or 401, navigate back to login screen
- Session timeout: not auto-logout for personal workstations; shared kiosks → configurable idle timeout (future enhancement, stubbed as a no-op for v1)

### 4. Role-based navigation after login

After successful auth, `AppShell` reads `Enum_UserRole` from the JWT claims (or from the user model returned by the login endpoint) and shows the correct flyout items:

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
- **Server dependency:** `check-workstation` and `auto-login` must be added to `MTM_Waitlist_Server` before the full workstation-aware auth flow can replace the initial manual login slice.
