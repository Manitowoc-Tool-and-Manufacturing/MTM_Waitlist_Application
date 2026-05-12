# User Guide Draft — Authentication & Login

## Purpose

This draft explains how end users sign in to the MTM Waitlist Application on personal workstations and shared floor terminals.

---

## Login Modes

### Personal workstation

If your Windows account is mapped to an active MTM Waitlist user and the computer is not registered as a shared workstation, the app attempts to sign you in automatically when it opens.

What you will see:

- the login screen may appear briefly while the app checks the workstation type
- if auto-login succeeds, the app opens directly to the main application
- if auto-login fails, the standard sign-in form remains available

### Shared workstation or floor terminal

If the machine is registered as a shared workstation, the app does not sign in automatically. You must enter your MTM Waitlist username and password or floor PIN.

What you will see:

- the sign-in form stays visible
- enter your username in the `Windows Username` field if instructed by your supervisor or local process documentation
- enter your password or floor PIN in the `Password / PIN` field
- select `Sign In`

---

## Stored Sessions

When a sign-in succeeds, the application stores a secure session locally. If that session is still valid the next time the app launches, the login screen is skipped and the app opens directly.

The application stores:

- access token
- refresh token
- token expiry timestamp
- current role

These values are stored using platform secure storage and are not written to a plain-text file.

---

## Common Errors

### Connection refused

If you see a message like `No connection could be made because the target machine actively refused it`, the client application could not reach the REST API.

Typical causes:

- the MTM Waitlist Server Admin app is not running
- the API is not listening on the configured address
- you are off the internal network and no local development API is running

### Invalid username or password

This means the server could not validate the supplied application credentials. Check for typing mistakes or confirm that your account is active.

---

## Support Notes

If login fails repeatedly, collect the following before contacting support:

- whether the machine is personal or shared
- your Windows username
- the exact error text shown on the sign-in page
- whether the server admin application is currently running

---

## Status

This is a draft user-guide page created during FEATURE-01 implementation and should be expanded during the formal User Guide phase.