Subject: MTM Waitlist Application — Progress Update, May 10, 2026

Team,

Good progress today on the Waitlist Application. Work this afternoon focused on building out the database foundation, and this evening was spent finalizing the design plans for the new server management tool that IT will use to run and maintain the application. Here is a plain summary of where things stand.

---

## THIS AFTERNOON — Database Foundation Built

The underlying database structure for the application is now fully defined and saved. This is the groundwork everything else is built on top of.

### User Accounts

The system now has a formal concept of user roles. Each person using the application will be assigned one of five roles — Press Operator, Material Handler, Lead, Admin, or Developer — and the database enforces those distinctions. Shared kiosk stations (tablets or screens used by multiple people without individual logins) are also supported with their own PIN-based access.

### Waitlist Entries

The core record the application manages — a waitlist entry — is now fully defined. It captures everything relevant to a material handling request: which press, which workcenter, which part, which operation, who is the operator, who is the assigned handler, priority level, current status, and a complete timestamp history.

### Security and Session Management

When a user logs in, the application issues a secure session token. The database tracks these tokens and can revoke them individually or all at once — for example, if a user's account is deactivated or a device is removed.

### Automatic Record Keeping

Every record in the database automatically stamps when it was created and when it was last changed. This happens in the database itself, so there is no risk of that information being missing or incorrect regardless of how the data is entered.

### Sample Data for Testing

A set of test users and sample waitlist entries was created so I can test the application against realistic data without using any real production information.

### Developer Tooling

The AI-assisted development tools were fully updated to match the actual state of the project. This keeps the AI working accurately — it now knows exactly what has been built, what naming conventions are in use, and what the rules of the project are. An audit was done beforehand to make sure the AI was not carrying any incorrect assumptions from earlier sessions.

---

## THIS EVENING — Server Management Tool Design Finalized

The Waitlist Application requires a dedicated program running on the server at all times — this is what serves the app to every tablet and kiosk on the floor. Tonight, the complete design for that server management tool was finished. This is an IT-facing desktop application (not something operators or handlers will see) that will run on the server machine and give IT full control over the system.

Six design areas were planned and all open questions were answered:

### 1. Overall Architecture

The server tool will run quietly in the background on the server starting automatically when the machine boots. IT can open it at any time to see what is happening and make changes. It hosts the application itself, meaning if this program is not running, the Waitlist Application will not work on the floor.

### 2. Live Health Dashboard

When IT opens the server tool, they will see a live dashboard showing: whether the application is online, how many floor devices are connected, how much data is in the database, and a real-time log of recent activity. This screen refreshes automatically every 30 seconds.

### 3. Settings

All configuration for the system lives in one place — the Settings screen. This includes database connection details, the port the application listens on, backup folder location, and the connection to the Infor Visual ERP system. All passwords and sensitive credentials are encrypted on disk. If IT changes the port or restarts the service, floor devices are automatically notified with a countdown warning so operators are not cut off without notice. Floor devices will automatically find the server again after a restart — no changes needed on the devices themselves.

### 4. Backup and Restore

The server tool performs a full database backup every night at 2:00 AM automatically and keeps 30 days of backups on hand. IT can also trigger a backup manually at any time with one click. If a restore is ever needed, the tool walks IT through the process: it automatically notifies all floor devices with a countdown (at minimum 60 seconds), waits for them to disconnect, performs the restore, and brings the system back online. There is no way to accidentally skip the warning to operators.

### 5. Remote Device Shutdown (Kill Switch)

IT can remotely close the Waitlist Application on any or all floor devices from the server tool. This is used before maintenance windows. When a shutdown is sent, the device shows a non-dismissable countdown to the operator so they are not caught off guard. IT can target a specific machine, a specific user, or all devices at once. The shutdown controls automatically disable themselves while a restore is in progress to prevent conflicts.

### 6. Database Updates (Migration System)

When a future version of the software requires changes to the database structure — for example, adding a new field to the waitlist record — those changes are applied through a controlled update system rather than manual database edits. The server tool tracks exactly which updates have been applied, applies new ones in the correct order, and prevents two instances from running updates at the same time. IT can choose to have updates apply automatically when the server tool starts, or apply them manually with a button. Manual approval will be the default for production.

---

## KEY DECISIONS CONFIRMED TONIGHT

Below are the business-level decisions that were confirmed during the planning session:

- The Infor Visual ERP connection is read-only. The Waitlist Application can read information from Visual (workcenter data, parts, operations, etc.) but will never write back to it. All of that access goes through the server — no floor device ever talks to Visual directly.
- When a database restore is needed, operators will always receive at least 60 seconds of warning. There is no way for IT to skip this.
- Backup files do not need to be encrypted. The backup folder will be restricted to IT accounts through Windows file permissions, which is sufficient.
- Operator-to-operator shutdown (a lead or supervisor remotely closing another operator's screen) is not needed in this version.
- The system does not need to send email alerts if an automatic backup fails. A warning message in the IT dashboard is sufficient.
- No audit log is needed for settings changes in this version. The activity log visible in the dashboard is sufficient.

---

## WHAT IS COMING NEXT

The design phase for the server management tool is complete. I can now begin building it. The planned build order is:

1. Create the server tool project and its basic structure
2. Set up the database update system (must happen before any new database changes are made)
3. Build the live dashboard and settings screens
4. Build the backup/restore and device shutdown features

In parallel, eight end-user feature designs (the screens operators and material handlers will actually use) are also documented and waiting for review. Those cover login, the request wizard, the live queue, zone assignments, the lead dashboard, setup technicians, and quality control.

---

John