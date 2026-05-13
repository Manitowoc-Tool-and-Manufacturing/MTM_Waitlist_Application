using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTM_Waitlist_Server.Core.Interfaces.KillSwitch;
using MTM_Waitlist_Server.Core.Models.KillSwitch;

namespace MTM_Waitlist_Server.Api.Controllers;

/// <summary>
/// Endpoints for admin operations: shutdown signals, client heartbeats, and connected-client queries.
/// MAUI clients poll GET /api/admin/shutdown-signal every 15 seconds.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IService_KillSwitch _killSwitch;

    public AdminController(IService_KillSwitch killSwitch)
    {
        _killSwitch = killSwitch;
    }

    /// <summary>Returns the shutdown signal applicable to the calling client, or 204 if none.</summary>
    [HttpGet("shutdown-signal")]
    public IActionResult GetShutdownSignal(
        [FromQuery] string machineName,
        [FromQuery] string username)
    {
        var signal = _killSwitch.GetSignalForClient(machineName, username);
        if (signal is null)
        {
            return NoContent();
        }

        return Ok(signal);
    }

    /// <summary>Clears the shutdown signal after a client has received it.</summary>
    [HttpPost("shutdown-signal/acknowledge")]
    public IActionResult AcknowledgeShutdownSignal([FromBody] AcknowledgeShutdownSignalRequest request)
    {
        _killSwitch.AcknowledgeSignalForClient(request.MachineName, request.Username);
        return Ok();
    }

    /// <summary>Records a client heartbeat so the admin UI knows the client is connected.</summary>
    [HttpPost("heartbeat")]
    public IActionResult PostHeartbeat([FromBody] HeartbeatRequest request)
    {
        _killSwitch.RecordHeartbeat(request.MachineName, request.Username,
            request.FullName, request.WorkstationName);
        return Ok();
    }

    /// <summary>Removes a client heartbeat when the client disconnects normally.</summary>
    [HttpPost("disconnect")]
    public IActionResult PostDisconnect([FromBody] ClientDisconnectRequest request)
    {
        _killSwitch.RemoveHeartbeat(request.MachineName, request.Username);
        return Ok();
    }

    /// <summary>Issues a shutdown signal. Requires Admin or Developer role.</summary>
    [HttpPost("shutdown")]
    [Authorize(Roles = "Admin,Developer")]
    public IActionResult SetShutdown([FromBody] SetShutdownRequest request)
    {
        if (_killSwitch.IsRestoreInProgress)
        {
            return Conflict("A restore is in progress. Shutdown signals are disabled.");
        }

        _killSwitch.SetShutdownSignal(
            request.Target,
            request.WarningSeconds,
            request.Message,
            request.MachineName,
            request.Username);

        return Ok();
    }

    /// <summary>Cancels all active shutdown signals. Requires Admin or Developer role.</summary>
    [HttpDelete("shutdown")]
    [Authorize(Roles = "Admin,Developer")]
    public IActionResult CancelShutdown()
    {
        _killSwitch.CancelAllSignals();
        return Ok();
    }

    /// <summary>Returns the list of currently connected clients. Requires Admin or Developer role.</summary>
    [HttpGet("clients")]
    [Authorize(Roles = "Admin,Developer")]
    public IActionResult GetConnectedClients()
    {
        return Ok(_killSwitch.GetConnectedClients());
    }
}

/// <summary>Request body for POST /api/admin/heartbeat.</summary>
public record HeartbeatRequest(
    string MachineName,
    string Username,
    string FullName,
    string? WorkstationName);

/// <summary>Request body for POST /api/admin/disconnect.</summary>
public record ClientDisconnectRequest(
    string MachineName,
    string Username);

/// <summary>Request body for POST /api/admin/shutdown-signal/acknowledge.</summary>
public record AcknowledgeShutdownSignalRequest(
    string MachineName,
    string Username);

/// <summary>Request body for POST /api/admin/shutdown.</summary>
public record SetShutdownRequest(
    ShutdownTarget Target,
    int WarningSeconds,
    string Message,
    string? MachineName,
    string? Username);
