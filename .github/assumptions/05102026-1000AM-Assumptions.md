# Database Schema Assumptions

**File:** `05102026-1000AM-Assumptions.md`
**Date:** May 10, 2026
**Task:** Create base MySQL database schema, stored procedures, triggers, and indexes
**Status:** ✅ ALL ASSUMPTIONS CONFIRMED — May 10, 2026
**Applied to:** All SQL files in `Database/` and C# model/enum files in Core project

---

## Context

`Model_WaitlistEntry` in the codebase currently contains only `Id (int)`, with the
explicit comment:

> *"Additional fields will be added once the API schema is finalized."*

No `Enum_WaitlistStatus`, no additional entity fields, and no domain-specific models
beyond `Model_AuthToken` exist. The API endpoints (`/api/waitlist`, `/api/auth/login`,
`/api/auth/refresh`) are defined but the data shape is not.

The following assumptions were required to produce a functional base schema. The database
files are clearly annotated with `⚠️ ASSUMPTION` markers. Review and correct before
applying to production.

---

## Confirmed Answers (May 10, 2026)

| # | Assumption | Original Assumption | Confirmed Answer | Status |
|---|-----------|---------------------|-----------------|--------|
| 1 | WaitlistEntry fields | Person-based (FirstName, LastName, etc.) | **Workcenter-based** — a workcenter waiting on a logistics request (Coil, Dunnage, picking up goods, die handling, etc.) | ✅ Corrected |
| 2 | Status ENUM values | `Waiting, Active, Completed, Cancelled` | Add `Late`, `LowImportance`, `Project` | ✅ Confirmed + Extended |
| 3 | Authentication | Internal Users table only | **Mixed** — Windows usernames on a SharedWorkstations list require app login; all other Windows usernames auto-login | ✅ Corrected |
| 4 | User roles | `Admin, Manager, User` | `PressOperation, SetupTech, ProductionSupervisor, ProductionManager, Quality, Receiving, MaterialHandler, Admin, Developer` | ✅ Corrected |
| 5 | Password hashing | bcrypt / VARCHAR(256) | bcrypt / VARCHAR(256) | ✅ Confirmed |
| 6 | Database name | `MTM_Waitlist` (PascalCase) | `mtm_waitlist` — **all MySQL database names must be lowercase** | ✅ Corrected |
| 7 | MySQL version | 8.0+ | **MySQL 5.7** | ✅ Corrected |

---

## Detail: WaitlistEntry Request Types (Assumption 1)

A WaitlistEntry represents a **workcenter** submitting a logistics/material-handling request.
The `RequestType` column captures what the workcenter needs:

| RequestType value | Description |
|------------------|-------------|
| `Coil` | Deliver a coil |
| `Dunnage` | Deliver dunnage |
| `PickUpFinishedGoods` | Pick up finished goods from the workcenter |
| `PickUpUnusedGoods` | Pick up unused goods from the workcenter |
| `PickUpDunnage` | Pick up dunnage from the workcenter |
| `BringPartsToPress` | Bring parts to the press |
| `RemoveCoilFromPress` | Remove a coil from the press |
| `BringPickUpDie` | Bring or pick up a die |

Removed from schema: `FirstName`, `LastName`, `PhoneNumber`, `Department`, `Position`
Added to schema: `WorkcenterName`, `RequestType`

## Detail: Authentication Flow (Assumption 3)

1. App reads the current Windows username on startup.
2. App calls `usp_Auth_CheckSharedWorkstation(windowsUsername)`.
3. If the Windows username **is** in `SharedWorkstations` → show login form → validate via `usp_Auth_ValidateCredentials`.
4. If the Windows username **is not** in `SharedWorkstations` → auto-login via `usp_Auth_GetUserByWindowsUsername(windowsUsername)`.

New table required: `SharedWorkstations` — stores Windows usernames of PCs that require manual login.
New column on `Users`: `WindowsUsername VARCHAR(100) NULL UNIQUE` — maps a personal Windows login to an app user for auto-login.

---

## Original Assumptions (for reference)

### 1. WaitlistEntry Business Fields

**Assumption:** A waitlist entry represents a **person** waiting for a resource, position,
training slot, or service at MTM. The schema includes:

| Column | Type | Reason assumed |
|--------|------|----------------|
| `FirstName` | VARCHAR(100) | Person's first name |
| `LastName` | VARCHAR(100) | Person's last name |
| `PhoneNumber` | VARCHAR(20) | Contact info |
| `Department` | VARCHAR(100) | MTM department (Production, QA, etc.) |
| `Position` | VARCHAR(150) | What they are waiting for |
| `Status` | ENUM | Current state in the waitlist process |
| `Priority` | TINYINT | Ordering (1 = highest, 10 = lowest) |
| `Notes` | TEXT | Free-text remarks |
| `RequestedAt` | DATETIME | UTC — when they joined the waitlist |
| `ScheduledAt` | DATETIME | UTC — estimated service/start date |
| `CompletedAt` | DATETIME | UTC — when entry was resolved |
| `CreatedByUserId` | INT FK | Audit — who created the record |
| `UpdatedByUserId` | INT FK | Audit — who last modified the record |

**Why needed:** `Model_WaitlistEntry` has no business fields. Without them, no meaningful
table can be created.

**Impact if wrong:** Business-specific columns (`FirstName`, `LastName`, `Department`,
`Position`) would be replaced or renamed. Infrastructure columns (`Id`, `CreatedAt`,
`UpdatedAt`, `CreatedByUserId`) are likely correct regardless of business domain.

**Alternatives considered:**
- Customer/service waitlist — would need `CustomerId`, `ServiceType` references
- Equipment/machine access waitlist — would need `EquipmentId`, `MachineId` references
- Internal job opening waitlist — would need `JobId`, `PositionCode` references
- Work order processing queue — would need `WorkOrderId`, `JobNumber` references

---

### 2. Status ENUM Values

**Assumption:** Status values are: `Waiting`, `Active`, `Completed`, `Cancelled`

**Why needed:** No `Enum_WaitlistStatus` exists in the codebase. A MySQL ENUM column
requires fixed values at table creation time.

**Impact if wrong:** An `ALTER TABLE` statement would replace the ENUM values. If the
set grows substantially, a separate `WaitlistStatuses` lookup table would be more
maintainable.

**Alternatives considered:**
- `Pending`, `InProgress`, `Done`, `Rejected` — different business language
- `Open`, `InReview`, `Approved`, `Declined`, `Archived` — approval-workflow framing
- A separate `WaitlistStatuses` reference table — more flexible, adds joins

---

### 3. User Authentication — Internal Users Table

**Assumption:** Application users (staff who manage the waitlist) are stored in a `Users`
table within the `MTM_Waitlist` database. Refresh tokens are persisted in a
`RefreshTokens` table.

**Why needed:** `/api/auth/login` and `/api/auth/refresh` endpoints exist and the
application uses JWT (confirmed by `Model_AuthToken.Token` and `ExpiresAt`). A
persistence layer is needed for credentials and token lifecycle management.

**Impact if wrong:**
- If MTM uses **Active Directory / Windows authentication**: the `Users` table and all
  `usp_Auth_*` procedures would be removed or replaced with LDAP lookup logic in the API.
- If using an **external OAuth/OIDC provider** (Entra ID, Okta): auth tables are not
  needed at all; only the waitlist tables would remain.
- If using **stateless JWT only** (no refresh tokens): the `RefreshTokens` table and
  related procedures are not needed.

**Alternatives considered:** LDAP/AD integration, OAuth2/OIDC, stateless JWT with
short-lived tokens only.

---

### 4. User Roles

**Assumption:** Three roles exist: `Admin`, `Manager`, `User`

**Why needed:** A managed waitlist system implies role-based access control.

**Impact if wrong:** The ENUM definition in `Users` must be altered to match the real
role set.

**Alternatives considered:** A separate `Roles` table with a many-to-many `UserRoles`
mapping (more flexible, adds complexity).

---

### 5. Password Hashing Algorithm

**Assumption:** The API backend uses **bcrypt** for password hashing. The `PasswordHash`
column is `VARCHAR(256)`.

**Why needed:** The column must be sized correctly. Bcrypt produces 60-character hashes;
`VARCHAR(256)` safely accommodates bcrypt, Argon2, and scrypt.

**Impact if wrong:** Column size may need adjustment. Algorithm-specific handling is
in the API backend, not in MySQL.

**Alternatives considered:** Argon2 (recommended for new systems), PBKDF2, SHA-256
(not recommended without salting).

---

### 6. Database Name

**Assumption:** Database name is `MTM_Waitlist`, following the project naming convention
(`MTM_Waitlist_Application`, `MTM_Waitlist_Application.slnx`).

**Why needed:** Every SQL script requires a target database name.

**Impact if wrong:** A global find-and-replace on `` `MTM_Waitlist` `` resolves this
across all SQL files.

---

### 7. MySQL Version

**Assumption:** MySQL **8.0 or later** is running on `172.16.1.104`.

**Why needed:** SQL syntax, ENUM handling, stored procedure features, and
`utf8mb4_unicode_ci` behavior vary by version.

**Impact if wrong:** Minor syntax adjustments may be needed for MySQL 5.7.

---

## Request for Confirmation

Before applying these files to the server, please confirm or correct:

1. **What does a WaitlistEntry represent at MTM?**
   What fields does it actually need beyond `Id`?

2. **What Status values does the business process use?**
   (e.g., `Waiting / Active / Completed / Cancelled` or something different)

3. **How are application users authenticated?**
   - Internal `Users` table (as assumed here)
   - Active Directory / Windows Authentication
   - External OAuth/OIDC provider

4. **What user roles exist?**
   Is `Admin / Manager / User` correct?

5. **Is the database name `MTM_Waitlist` correct?**

6. **What version of MySQL is on `172.16.1.104`?**

**Once confirmed:** Update `Model_WaitlistEntry` in the C# codebase and align the
`WaitlistEntries` MySQL table simultaneously, then expand `Entity_WaitlistEntry`
(SQLite cache entity) to match.
