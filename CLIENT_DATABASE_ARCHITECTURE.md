# Client-to-Database Architecture Note

For this application, both the **Android** and **Windows** clients should **not connect directly to the MySQL database**.

## Recommended Architecture

Use this pattern instead:

**Client App (Android / Windows) ⇄ API / Backend ⇄ MySQL**

## Why direct database access is not recommended

Direct database connections from client apps create several problems:

- Database credentials would need to be stored in the client app
- Credentials can be extracted through reverse engineering or device compromise
- Business rules and validation would be duplicated or bypassed
- Database access would need to be exposed to end-user devices
- Logging, auditing, and rate limiting are harder to enforce
- Future app changes become harder to manage across platforms

## How the app should work

### Android and Windows clients
The app should:
- Send HTTPS requests to a backend API
- Authenticate users if needed
- Submit and retrieve waitlist data through the API
- Never store or use direct MySQL credentials

### Backend API
The backend should:
- Authenticate and authorize requests
- Validate incoming data
- Apply business logic
- Read and write waitlist records in MySQL
- Return safe API responses to the client apps

## Benefits of this approach

- Better security
- One place for business logic
- Easier maintenance
- Shared backend for Android and Windows
- Easier expansion to web or iOS later

## Optional enhancement

If offline support is needed later, the client app can use a local cache such as **SQLite** and sync with the backend when online.

## Summary

For both Android and Windows, the correct long-term approach is:

**Client App ⇄ Secure Web API ⇄ MySQL**

This keeps MySQL protected and makes the application easier to maintain as it grows.
