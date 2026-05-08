# Remote Client Access to Work-Hosted API

Yes, the application can be installed on a **home PC** or used from an **Android device at home** and still connect to a **work-hosted backend API**, as long as the API is reachable from that location.

## Key idea

The client app does **not** need to be on the same network as the backend API.
It only needs a valid and secure network path to reach the API.

## Common ways this can work

### 1. Public HTTPS API
This is the most common setup for remote access.

- The backend API is hosted at work or in the cloud
- It is exposed through a secure public URL
- Example: `https://api.companyname.com`
- The Windows app and Android app can call the API from home over the internet

### 2. VPN access to the company network
This is a good option when the API should remain internal.

- The user connects to the company VPN
- The app then reaches the internal API through that VPN connection
- This keeps the API off the public internet while still allowing remote use

### 3. Internal-only API with no remote path
If the API is only reachable from inside the company network and there is:

- no public HTTPS endpoint
- no VPN access
- no remote access solution

then the app will **not** be able to connect from home.

## Requirements for home access

At least one of the following must be true:

- The API is publicly reachable over HTTPS
- The device can connect into the work network through a VPN

## Security recommendations

If the app will be used from home or other remote locations, the backend API should use:

- HTTPS
- Authentication
- Authorization
- Logging
- Secure token handling
- Optional IP restrictions or VPN-only access depending on company policy

## Example

- API hosted by company: `https://waitlist-api.company.com`
- Windows app on a home PC connects to that API
- Android app at home connects to that API
- The API then communicates with the MySQL database at work or in the cloud

## Summary

A home PC install or Android device can connect to a work-based backend API if the API is exposed securely through:

- a public HTTPS endpoint, or
- a VPN connection into the company network

Without one of those options, the app will not be able to reach the backend from home.
