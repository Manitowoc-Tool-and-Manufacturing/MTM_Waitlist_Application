# Remote Database Access — Implementation Plan
**Date:** May 9, 2026
**Targets:** Windows (WinUI) + Android (Droid)

---

## Architecture

```
WinUI / Android (on work LAN — wired or wireless)
    ⇄  HTTPS + JWT  ⇄  REST API (internal)  ⇄  MySQL
    ⇄  SQLite (offline queue — syncs when LAN access is restored)
```

---

## Codebase Context

- WinUI 3 (Windows) + .NET MAUI (Android) dual-host solution
- No backend API exists yet — all HTTP implementations are stubs until API is defined
- All projects contain only `Class1.cs` stubs
- `MauiProgramExtensions.cs` DI framework exists (all registrations commented out)
- Android `INTERNET` permission exists (duplicated — needs cleanup)
- Auth strategy: JWT bearer tokens (stateless, works on work LAN — wired and wireless)
- API URL storage: `appsettings.json` embedded in Shared project (never a string literal)
- API is internal-only — accessible on the work LAN, not exposed to the public internet
- Offline strategy: SQLite local cache + `IConnectivity`-driven sync service (handles Wi-Fi drops on the work network)

---

## Reference Chain to Add

| Project | May reference |
|---------|--------------|
| Shared | Data, Services, Core, Feature.* |
| Feature.* | Services, Core only |
| Services | Core only |
| Data | Core only |
| Core | Nothing |

---

## Phase 1 — Core Models & Interfaces
**Project:** `Core`

- [ ] `Models/Waitlist/Model_WaitlistEntry.cs` — placeholder entity (fields added when API is defined)
- [ ] `Models/Shared/Model_Dao_Result.cs` — two variants, both needed:
  - `Model_Dao_Result<T>` — for operations that return data (get, list)
  - `Model_Dao_Result` (non-generic) — for void operations (insert, update, delete confirmations)
- [ ] `Models/Auth/Model_AuthToken.cs` — JWT string + expiry timestamp
- [ ] `Interfaces/Api/IApiClient.cs` — HTTP GET/POST/PUT/DELETE returning `Model_Dao_Result<T>`
- [ ] `Interfaces/Auth/IService_Auth.cs` — login / logout / token refresh contract
- [ ] `Interfaces/Waitlist/IRepository_WaitlistEntry.cs` — online data access contract (API calls)
- [ ] `Interfaces/Waitlist/IRepository_WaitlistEntryLocal.cs` — offline data access contract (SQLite only)
- [ ] `Interfaces/Waitlist/IService_WaitlistEntry.cs` — business logic contract
- [ ] `Interfaces/Sync/ISyncService.cs` — offline queue flush contract
- [ ] `Constants/Api/Constants_Api.cs` — placeholder base URL constant

---

## Phase 2 — Data Layer
**Project:** `Data`

- [ ] Add NuGet `sqlite-net-pcl` to `Data.csproj`
- [ ] `Http/HttpApiClient.cs` — implements `IApiClient`
  - Base URL from `IConfiguration["Api:BaseUrl"]`
  - JWT read from `SecureStorage.GetAsync("auth_token")` → `Authorization: Bearer` header
  - Uses `System.Net.Http.Json` (built into .NET 10, no extra package)
  - Registered via `IHttpClientFactory`
- [ ] `Local/LocalDbContext.cs` — `SQLiteAsyncConnection` wrapper for offline SQLite
- [ ] `Repositories/Waitlist/Repository_WaitlistEntry.cs` — implements `IRepository_WaitlistEntry`
  - Online only — calls `IApiClient` and returns `Model_Dao_Result`
  - No connectivity logic here — pure API data access
  - All `HttpRequestException` (including mid-request drops) caught internally and returned as `Model_Dao_Result.Failure` — never thrown
- [ ] `Repositories/Waitlist/Repository_WaitlistEntryLocal.cs` — implements `IRepository_WaitlistEntryLocal`
  - Offline only — reads/writes `LocalDbContext` (SQLite) and returns `Model_Dao_Result`
  - No connectivity logic here — pure local data access

---

## Phase 3 — Services Layer
**Project:** `Services`

- [ ] `Auth/Service_Auth.cs` — implements `IService_Auth`
  - Login → JWT → `SecureStorage.SetAsync("auth_token", token)`
  - Logout → `SecureStorage.Remove("auth_token")`
  - Token stored in platform keystore (Android Keystore / Windows DPAPI) — never plaintext
- [ ] `Waitlist/Service_WaitlistEntry.cs` — implements `IService_WaitlistEntry`
  - Checks `IConnectivity.NetworkAccess` before each operation to pick the right repository
  - Online: delegates to `IRepository_WaitlistEntry` (API)
  - Offline: delegates to `IRepository_WaitlistEntryLocal` (SQLite); queues writes for sync
  - If `IRepository_WaitlistEntry` returns `Model_Dao_Result.Failure` (mid-request drop), automatically falls back to `IRepository_WaitlistEntryLocal`
- [ ] `Sync/SyncService.cs` — implements `ISyncService`
  - Subscribes to `IConnectivity.ConnectivityChanged`
  - On reconnect: flushes offline write queue through `IApiClient`
  - ⚠️ Must be resolved eagerly at app startup so the event subscription is active before any screen loads (see Phase 5)

---

## Phase 4 — Configuration
**Project:** `MTM_Waitlist_Application` (Shared)

- [ ] Add `appsettings.json` as `EmbeddedResource` in `MTM_Waitlist_Application.csproj`
  ```json
  {
    "Api": {
      "BaseUrl": "https://PLACEHOLDER"
    }
  }
  ```
  The `.csproj` must include an explicit item group — without it the file is excluded at build and `AddJsonStream()` will throw a null reference at startup:
  ```xml
  <ItemGroup>
    <EmbeddedResource Include="appsettings.json" />
    <EmbeddedResource Include="appsettings.Development.json"
                      Condition="'$(Configuration)'=='Debug'" />
  </ItemGroup>
  ```
- [ ] Add `appsettings.Development.json` override for local testing
- [ ] In `MauiProgramExtensions.UseSharedMauiApp()`: load `IConfiguration` from embedded stream — wrap in a `using` block to prevent a memory leak on Android:
  ```csharp
  using var stream = Assembly.GetExecutingAssembly()
      .GetManifestResourceStream("MTM_Waitlist_Application.appsettings.json")!;
  builder.Configuration.AddJsonStream(stream);
  ```
- [ ] Register `IHttpClientFactory` via `builder.Services.AddHttpClient()`

---

## Phase 5 — DI Wiring
**Files:** `MauiProgramExtensions.cs` + `MTM_Waitlist_Application.csproj`

- [ ] Add `<ProjectReference>` entries to Shared `.csproj` for Data, Services, Core, Feature.*
- [ ] In `AddSharedServices()`, register:

| Interface | Implementation | Lifetime |
|-----------|---------------|----------|
| `IConnectivity` | `Connectivity.Current` (MAUI built-in — used by shared Services layer; resolves on both Android and Windows via MAUI NuGet reference in `Services.csproj`) | Singleton |
| `IApiClient` | `HttpApiClient` | Singleton |
| `LocalDbContext` | `LocalDbContext` | Singleton |
| `IRepository_WaitlistEntry` | `Repository_WaitlistEntry` | Singleton |
| `IRepository_WaitlistEntryLocal` | `Repository_WaitlistEntryLocal` | Singleton |
| `IService_Auth` | `Service_Auth` | Singleton |
| `IService_WaitlistEntry` | `Service_WaitlistEntry` | Singleton |
| `ISyncService` | `SyncService` | Singleton |
| ViewModels / Pages | (as features are built) | Transient |

> ⚠️ **`SyncService` must be resolved eagerly** — add the following after `app = builder.Build()` in `UseSharedMauiApp()` so the `ConnectivityChanged` subscription is active before any screen loads:
> ```csharp
> app.Services.GetRequiredService<ISyncService>();
> ```

---

## Phase 6 — Android Manifest
**File:** `MTM_Waitlist_Application.Droid/AndroidManifest.xml`

- [ ] Remove duplicate `INTERNET` permission (currently listed twice)
- [ ] Enforce HTTPS by adding a `Resources/xml/network_security_config.xml` file — this allows cleartext HTTP in Debug only (needed for local dev API on the emulator) while blocking it in Release:
  ```xml
  <?xml version="1.0" encoding="utf-8"?>
  <network-security-config>
    <debug-overrides>
      <base-config cleartextTrafficPermitted="true" />
    </debug-overrides>
    <base-config cleartextTrafficPermitted="false" />
  </network-security-config>
  ```
  Reference it in `AndroidManifest.xml`: `<application android:networkSecurityConfig="@xml/network_security_config">`
  > ⚠️ Do **not** use `android:usesCleartextTraffic="false"` directly on `<application>` — it applies to both Debug and Release, which breaks all HTTP traffic to a local development API on the emulator.

---

## Phase 7 — Verification

- [ ] `dotnet build MTM_Waitlist_Application.slnx` — zero errors
- [ ] Confirm `Feature.*` projects have no `<ProjectReference>` to `Data`
- [ ] Confirm `Services` project has no `<ProjectReference>` to `Data` (only `Core`)
- [ ] Confirm `HttpClient` base address comes from `IConfiguration`, not a string literal
- [ ] Confirm `SecureStorage` is the only token store (grep for `Preferences.Set`, plaintext credential patterns)
- [ ] Confirm both host `MauiProgram.cs` files call only `UseSharedMauiApp()` — no extra registrations
- [ ] Confirm `SyncService` is resolved eagerly at startup (grep for `GetRequiredService<ISyncService>` in `UseSharedMauiApp()`)
