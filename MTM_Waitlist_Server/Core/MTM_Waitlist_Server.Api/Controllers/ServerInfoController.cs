using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTM_Waitlist_Server.Core.Interfaces.Settings;

namespace MTM_Waitlist_Server.Api.Controllers;

/// <summary>
/// Discovery endpoints called by MAUI clients on startup to resolve the API base URL
/// and check Visual proxy availability.
/// </summary>
[ApiController]
[Route("api/server-info")]
[Authorize]
public class ServerInfoController : ControllerBase
{
    private readonly IService_SettingsStore _settings;

    public ServerInfoController(IService_SettingsStore settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Returns the current Waitlist API host and port so MAUI clients can resolve the base URL
    /// dynamically without requiring a redeployment when the port changes.
    /// </summary>
    [HttpGet("waitlist")]
    public IActionResult GetWaitlistInfo()
    {
        var api = _settings.Get().Api;
        // Extract host:port from the listen URL (e.g. http://0.0.0.0:5000 → 172.16.1.104:5000).
        var uri = new Uri(api.ListenAddress.Replace("0.0.0.0", Request.Host.Host));
        return Ok(new
        {
            host = uri.Host,
            port = uri.Port,
            apiBaseUrl = $"http://{uri.Host}:{uri.Port}"
        });
    }

    /// <summary>
    /// Returns Visual SQL Server connection availability (server-internal only).
    /// Credentials are never returned — only the Enabled flag and connection status.
    /// </summary>
    [HttpGet("visual")]
    public IActionResult GetVisualInfo()
    {
        var visual = _settings.Get().Visual;
        return Ok(new { visual.Enabled, visual.Host, visual.Port, visual.DatabaseName });
    }
}
