# Copilot Instructions

## Project Guidelines
- User's IDE is Microsoft Visual Studio Community 2026 (version 18.5.2).
- The MTM Waitlist Application has two separate solutions in the same repository:
  - MAUI client solution: `C:\Users\johnk\source\repos\MTM_Waitlist_Application\MTM_Waitlist_Application\MTM_Waitlist_Application.slnx`
  - Server/Admin solution: `C:\Users\johnk\source\repos\MTM_Waitlist_Application\MTM_Waitlist_Server\MTM_Waitlist_Server.slnx`
- The Git remote for the MTM Waitlist Application repository is: `origin → https://github.com/Manitowoc-Tool-and-Manufacturing/MTM_Waitlist_Application` on branch `master`.
- The workspace root switches depending on which solution is open in VS.
- The MAUI client KillSwitch service (Service_KillSwitch.cs) is located at: `C:\Users\johnk\source\repos\MTM_Waitlist_Application\MTM_Waitlist_Application\Core\Services\KillSwitch\Service_KillSwitch.cs`. It runs a 15-second heartbeat loop, POSTs to `/api/admin/heartbeat`, polls `/api/admin/shutdown-signal`, and raises `ShutdownSignalReceived` on the main thread. It is registered as a Singleton in `MauiProgramExtensions.cs` and started from `App.xaml.cs` after `SwapToShell`.

## Build Instructions
- Always use `/nodeReuse:false` on all `dotnet build` terminal commands to prevent orphaned MSBuild worker processes from piling up in Task Manager. Example: `dotnet build MTM_Waitlist_Application.slnx /nodeReuse:false`