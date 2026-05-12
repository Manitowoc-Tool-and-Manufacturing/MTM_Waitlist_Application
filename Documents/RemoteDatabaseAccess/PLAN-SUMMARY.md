# Remote Database Access — Full Plan Summary
**Date:** May 9, 2026

---

## What We Are Building and Why

Right now the app has no way to read from or write to the company's waitlist records. This plan describes how we connect both the Windows desktop app and the Android phone app to that data — safely, while on the company's own network (wired or wireless).

The most important rule: **neither app ever talks directly to the company database.** Instead, both apps talk to a middleman service (called an [API](#api-application-programming-interface)) that sits between the apps and the database. This is the same approach used by virtually every modern app (banking apps, email clients, etc.) because it keeps the database locked away behind a secure door that only the middleman can open.

```
Windows App  ─┐
              ├──  Work Network (Wired or Wi-Fi)  ──  Middleman (API)  ──  Company Database (MySQL)
Android App  ─┘

Both apps also keep a small local copy of data on the device for use when network access is temporarily lost.
```

---

## How Network Access Works

Both apps connect while the device is on the company's own network:
- **Windows app** — connected to the company network via a wired ethernet cable or the office Wi-Fi
- **Android app** — connected to the company Wi-Fi

The middleman ([API](#api-application-programming-interface)) lives inside the company network and is not exposed to the public internet. No special setup is needed beyond being connected to the work network. If a device is not on the company network, the app will not be able to reach the server — but it can still show previously loaded data from its local copy.

---

## How Security Works

Every time a user logs in, the app receives a small digital pass called a [token](#jwt-json-web-token). Think of it like a temporary ID badge. Every request the app sends to the middleman includes that badge. If the badge is missing, expired, or fake, the middleman refuses the request. Badges expire automatically, so even if one were ever stolen it would stop working on its own.

The badge is stored in the phone or computer's built-in secure vault — the same secure area the operating system uses to store things like saved Wi-Fi passwords. It is never written to a plain file where it could be read easily.

---

## What Happens When Network Access Is Lost

If a device temporarily loses its connection to the company network (for example, a phone moves out of Wi-Fi range), the app does not crash or show an error. It silently switches to a small local copy of the data stored on the device (called a [local cache](#sqlite)). The user can still view records and make changes. As soon as the device reconnects to the work network, the app automatically sends any queued changes to the middleman to keep everything in sync.

If the network drops in the middle of a request that was already in progress — for example, the user tapped “Save” and the network cut out a fraction of a second later — the app catches that failure the same way. The change is saved locally and queued for sync, and the user sees a plain message rather than a crash.

---

## The Build Phases

### Phase 1 — Define the Shared Blueprint
*Sets up the core definitions that every other part of the app will follow.*

This phase creates the shared language of the app — definitions for what a waitlist record looks like, what a successful or failed result looks like, what a login token looks like, and the rules each piece of the system must follow. Nothing in this phase does any real work yet; it just defines the contracts everything else will honor.

**Files created in `Core/`:**

1. `Models/Model_WaitlistEntry.cs` — The template for what a single waitlist record contains. Fields will be filled in once the [API](#api-application-programming-interface) is defined.
2. `Models/Model_Dao_Result.cs` — A standard envelope every operation returns — either "here is your data" or "something went wrong, here is why." Used throughout the entire app so errors are handled consistently.
3. `Models/Model_AuthToken.cs` — Holds the login [token](#jwt-json-web-token) and when it expires.
4. `Interfaces/Api/IApiClient.cs` — The rulebook for how the app is allowed to talk to the middleman (send data, ask for data, update data, delete data).
5. `Interfaces/Auth/IService_Auth.cs` — The rulebook for how the app handles login, logout, and token refresh.
6. `Interfaces/Waitlist/IRepository_WaitlistEntry.cs` — The rulebook for how waitlist records are read and written (whether online or from the local cache).
7. `Interfaces/Waitlist/IService_WaitlistEntry.cs` — The rulebook for how the rest of the app asks for waitlist data without needing to know where it comes from.
8. `Interfaces/Sync/ISyncService.cs` — The rulebook for how queued offline changes get sent to the server when connection is restored.
9. `Constants/Api/Constants_Api.cs` — A placeholder for the middleman's web address, used during development before the real address is known.

---

### Phase 2 — Build the Data Layer
*The part of the app that actually sends and receives data.*

This phase builds the code that does the real work: sending requests to the middleman over the internet, and managing the local copy of data on the device for when there is no connection.

**Files created in `Data/`:**

10. Add the `sqlite-net-pcl` library to the project — this gives the app the ability to maintain a small [local database](#sqlite) on the device.
11. `HttpApiClient.cs` — The messenger that sends requests to the middleman over a secure connection ([HTTPS](#https)). It reads the middleman's address from the app's configuration file (so it can be changed without rewriting code), and it automatically attaches the user's login [token](#jwt-json-web-token) to every request so the middleman knows who is asking. If a request fails mid-flight because the network drops, it catches the error quietly and returns a safe “something went wrong” response — it never crashes the app.
12. `Local/LocalDbContext.cs` — Manages the small local copy of data stored on the device for when the work network is not available.
13. `Repositories/Waitlist/Repository_WaitlistEntry.cs` (online) and `Repositories/Waitlist/Repository_WaitlistEntryLocal.cs` (offline) — Two specialized record-keepers. The online one only knows how to talk to the middleman. The offline one only knows how to read and write the local copy on the device. Neither one decides which to use — that decision is made one layer up, in the Services layer.

---

### Phase 3 — Build the Services Layer
*The business logic that sits between the screen and the data.*

This phase builds the code that the app's screens talk to. It handles login, manages waitlist data requests, and watches for connectivity changes to trigger syncing.

**Files created in `Services/`:**

14. `Auth/Service_Auth.cs` — Handles everything related to the user's identity. When a user logs in, this sends the credentials to the middleman, receives the login [token](#jwt-json-web-token), and stores it in the device's secure vault. When the user logs out, it removes the token. If the token is close to expiring, it quietly requests a new one.
15. `Waitlist/Service_WaitlistEntry.cs` — The layer the app's screens talk to when they need waitlist information. Before passing a request along, it checks whether the device currently has network access and chooses which record-keeper to use — the online one when connected, the offline one when not. It also handles the situation where a request starts while connected but the connection drops halfway through, catching that failure silently and falling back to the local copy rather than showing the user an error or crash screen.
16. `Sync/SyncService.cs` — Watches the device's network connection. The moment the device reconnects to the work network after being offline, this automatically sends any changes that were saved locally while disconnected. To make sure it starts watching immediately when the app opens — not only after the first screen loads — it is activated as part of the app's very first startup steps.

---

### Phase 4 — Configuration
*Where the app's settings are stored.*

**Why not just write the server address directly into the code?**
If the address were written directly into the app's code, changing it would require modifying source files, rebuilding the entire app, and redeploying it. Instead, it lives in a separate settings file that can be swapped per environment (testing vs. production) without touching the code itself.

**Files created/modified in `MTM_Waitlist_Application/` (Shared project):**

17. `appsettings.json` — A small settings file bundled inside the app that holds the middleman's web address. During development this is a placeholder; before the app goes live it is replaced with the real address.
18. `appsettings.Development.json` — An override settings file used only during development so developers can point the app at a local test server without affecting the production settings.
19. `MauiProgramExtensions.cs` (updated) — Teaches the app how to read the settings file at startup, and registers the internet-communication tools with the app's service system.

---

### Phase 5 — Wiring Everything Together
*Connecting all the pieces so the app knows which component does what.*

Modern apps use a system where components declare what they need and the app automatically provides the right piece — instead of each component going out and finding its own tools. This phase registers every component built in the previous phases with that system.

**Files modified:**

20. `MTM_Waitlist_Application.csproj` (Shared) — Updated to include references to the Data, Services, Core, and all Feature projects so the Shared project can connect them all.
21. `MauiProgramExtensions.cs` — Every component is registered here so the app knows: "when something needs the internet messenger, give it `HttpApiClient`; when something needs login handling, give it `Service_Auth`" — and so on for every piece.

| What it does | Which component handles it | Shared or new per use |
|---|---|---|
| Sends internet requests | `HttpApiClient` | Shared — one instance for the whole app |
| Local offline database | `LocalDbContext` | Shared — one instance for the whole app |
| Reads/writes waitlist records | `Repository_WaitlistEntry` | Shared — one instance for the whole app |
| Login and token management | `Service_Auth` | Shared — one instance for the whole app |
| Waitlist business logic | `Service_WaitlistEntry` | Shared — one instance for the whole app |
| Offline sync on reconnect | `SyncService` | Shared — one instance for the whole app |
| Screen ViewModels and Pages | Added as features are built | New instance each time a screen opens |

---

### Phase 6 — Android Housekeeping
*Cleaning up a small issue in the Android app's configuration.*

**File modified:** `MTM_Waitlist_Application.Droid/AndroidManifest.xml`

22. The Android configuration file currently declares internet access permission twice by mistake. One of the duplicate entries will be removed.
23. A configuration file will be added that tells the Android system to only allow secure ([HTTPS](#https)) connections in the released version of the app, while still allowing plain connections during development so that developers can test against a local server on their own computer. Without this distinction, locking down HTTPS would prevent the app from connecting to a developer's local test setup during active development.

---

## Key Decisions and Why

| Decision | What was chosen | Why |
|---|---|---|
| **How apps get data** | Through a middleman [API](#api-application-programming-interface), never directly from the database | Keeps the database locked away; one place for all security and business rules |
| **How the connection is secured** | [HTTPS](#https) encryption on every request | Data cannot be read by anyone intercepting network traffic |
| **How users are identified** | A short-lived login [token](#jwt-json-web-token) sent with every request | Confirms the user's identity even on a shared work network; tokens expire automatically |
| **Where the token is stored** | The device's built-in secure vault ([SecureStorage](#securestorage)) | Never in a plain file; protected by the operating system the same way Wi-Fi passwords are |
| **Where the server address is stored** | A bundled settings file (`appsettings.json`) | Can be changed per environment without modifying code |
| **What happens offline** | App reads/writes a local [SQLite](#sqlite) copy; syncs automatically when back on the work network | Users are not blocked if Wi-Fi drops temporarily |
| **Waitlist record fields** | Placeholder for now | Fields will be defined once the middleman [API](#api-application-programming-interface) is built |

---

## Verification Checklist

Once all phases are complete, the following will be confirmed before the work is considered done:

- [ ] The entire solution builds with zero errors
- [ ] The screen/display code has no direct connection to the database layer — everything goes through the service layer
- [ ] The server address is read from the settings file, never written as a fixed value in code
- [ ] The login token is only ever stored in [SecureStorage](#securestorage) — not in any plain file or setting
- [ ] Both host apps (Windows and Android) delegate all setup to the shared configuration — no platform-specific registrations
- [ ] The Android configuration file has exactly one internet permission and enforces [HTTPS](#https)-only connections

---

## Glossary

### API (Application Programming Interface)
A middleman service that sits between the app and the database. The app sends requests to the API (e.g., "give me all waitlist entries"), the API checks that the request is valid and the user is allowed, and then it talks to the database. Neither the Windows app nor the Android app ever connects to the database directly. Think of it like a server at a restaurant — you tell the server what you want, the server goes to the kitchen, and the kitchen sends the food back through the server.

### HTTPS
The secure version of the standard internet communication protocol. The "S" stands for Secure. All data sent between the app and the API is encrypted so that anyone who might intercept the connection (e.g., on a public Wi-Fi network) sees only scrambled, unreadable noise. The app will be configured to refuse any connection that is not HTTPS.

### JWT (JSON Web Token)
A short-lived digital pass issued by the API when a user successfully logs in. It is a small piece of encoded text that the app attaches to every request it sends, proving the request comes from an authenticated user. It has an expiry time built in — once it expires the user must log in again (or the app quietly requests a new one). If a token were ever stolen, it would stop working on its own when it expires.

### LAN (Local Area Network)
The private network inside the company building — the wired ethernet connections and the office Wi-Fi. Only devices connected to this network can reach the internal middleman service (API) and the database. This is intentional: keeping the API off the public internet means it is only reachable by people physically present at the facility or connected to its Wi-Fi.

### NuGet
The package delivery system for .NET applications. When a developer needs to add a ready-made capability to a project (such as the ability to use a local SQLite database), they install a NuGet package rather than writing that capability from scratch. Think of it like an app store, but for code libraries used during development.

### SecureStorage
A built-in secure storage area provided by the operating system on every device. On Android it uses the Android Keystore; on Windows it uses a system called DPAPI. The app stores the user's login token here after they log in. It is the same protected area the operating system uses for things like saved Wi-Fi passwords — only the app that stored the data can read it back.

### SQLite
A lightweight, self-contained database that lives entirely in a single file on the device. Unlike the company's MySQL database (which runs on a server), SQLite requires no server — it is embedded directly in the app. It is used here as a local cache: when the device has no internet connection, the app reads from and writes to the SQLite copy, then syncs those changes to the server once connectivity is restored.

### Dependency Injection (DI)
A design pattern where components declare what they need, and the app automatically provides the right pre-built piece at runtime — instead of each component creating its own tools. For example, when the waitlist screen needs to load data, it simply asks for "a waitlist service" and the app hands it the correct one. This makes components interchangeable and easy to test. Phase 5 of this plan is where all components are registered with the DI system.

### Singleton vs. Transient (Component Lifetimes)
When a component is registered with the [DI](#dependency-injection-di) system, the app needs to know how many copies to keep. A **Singleton** means one shared copy is created once and reused by every part of the app for its entire lifetime (good for things like the internet messenger or the local database — there is no reason to have more than one). A **Transient** means a fresh copy is created each time it is needed. Screen ViewModels *must* be Transient — if a ViewModel were a Singleton, every screen that used it would share the same copy, meaning data entered on one screen could bleed into a different screen opened later. Each screen needs its own clean slate.

### Offline Queue
When the device has no internet connection and the user makes a change (such as adding a waitlist entry), that change cannot be sent to the API immediately. Instead it is saved to the local [SQLite](#sqlite) copy and added to a queue. When internet access is restored, the sync service works through the queue and sends each pending change to the API in order, keeping the server and the local copy in agreement.
