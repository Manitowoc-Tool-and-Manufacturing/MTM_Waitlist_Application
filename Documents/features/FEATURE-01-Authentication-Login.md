# FEATURE-01: Authentication & Login

**Status:** Ready to Implement  
**Priority:** Critical — Blocks Everything Else  
**Depends On:** Nothing (backend API + Service_Auth already exist)  
**Blocks:** All other features  

---

## Overview

The backend authentication layer is fully built: `Service_Auth`, `IService_Auth`, `/api/auth/login`, `/api/auth/refresh`, `/api/auth/auto-login`, `/api/auth/check-workstation`, `SecureStorage` JWT token handling, and the `Users` + `SharedWorkstations` MySQL tables are all defined.

What does **not** exist yet is any UI. This feature implements the login screen and session lifecycle that every other feature depends on.

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

### 1. `View_Auth_Login` (Feature.Waitlist or a new Feature.Auth project)

**Windows layout:**
- Full-screen centered card — not a navigation flyout item
- Two states driven by `ViewModel_Auth_Login.IsSharedWorkstation`:
  - **Auto-login in progress:** spinner + "Signing in as [Windows username]…"
  - **Credential entry:** username field + PIN pad (large tap targets, 48 px minimum)
- Error banner for failed logins (`InvalidCredentials`, `AccountLocked`, network failure)
- No "Remember Me" — JWT refresh handles session continuity

**Android layout:**
- Same two-state design, full-screen
- PIN pad buttons minimum 64 px for factory floor tap accuracy
- Keyboard should not auto-open (PIN pad is custom)

### 2. `ViewModel_Auth_Login`

```
[ObservableProperty] bool _isSharedWorkstation
[ObservableProperty] bool _isCheckingWorkstation   // spinner during /check-workstation
[ObservableProperty] bool _isAuthenticating        // spinner during login call
[ObservableProperty] string _username
[ObservableProperty] string _pin
[ObservableProperty] string _errorMessage

[RelayCommand] InitializeAsync()   // called OnAppearing — runs check-workstation flow
[RelayCommand] LoginAsync()        // credential submit
[RelayCommand] ClearPinAsync()     // reset PIN field
```

**Startup flow:**
```
InitializeAsync()
  → Service_Auth.CheckWorkstationAsync(Environment.MachineName)
  → if IsShared == false:
      Service_Auth.AutoLoginAsync(WindowsIdentity.GetCurrent().Name)
      → success → navigate to AppShell (role-based landing)
      → failure → show credential screen (fallback)
  → if IsShared == true:
      IsSharedWorkstation = true → show credential screen
```

### 3. Session lifecycle

- On successful login, `Service_Auth` stores JWT via `SecureStorage` — already implemented
- AppShell checks `SecureStorage` for a valid token on startup — if present and not expired, skip login
- Token refresh is handled by `Service_Auth.RefreshAsync()` — already implemented
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
| `Views/Login/View_Auth_Login.Windows.xaml` | Feature.Waitlist (or Feature.Auth) |
| `Views/Login/View_Auth_Login.Android.xaml` | Feature.Waitlist (or Feature.Auth) |
| `Views/Login/View_Auth_Login.xaml.cs` | Feature.Waitlist (or Feature.Auth) |
| `ViewModels/Login/ViewModel_Auth_Login.cs` | Feature.Waitlist (or Feature.Auth) |

---

## Open Decisions

- **Project placement:** Should auth live in `Feature.Waitlist` (simplest, no new project) or a dedicated `Feature.Auth`? Given the project is not yet large, `Feature.Waitlist` is recommended to avoid project-count bloat.
- **PIN vs password:** The original meeting used "badge + PIN". Current schema stores a `bcrypt` password hash. The UI should present a numeric PIN pad. The PIN is the user's password stored hashed — no separate PIN field needed in the schema.
- **Idle timeout for kiosks:** Stubbed out as no-op in v1. A future enhancement would monitor `PointerMoved`/`Tapped` events and auto-logout shared workstations after N minutes.
