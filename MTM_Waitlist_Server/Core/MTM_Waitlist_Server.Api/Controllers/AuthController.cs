using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MTM_Waitlist_Server.Api.Controllers;

/// <summary>
/// Authentication endpoints — login, token refresh, and revocation.
/// Implementation is a TODO stub pending FEATURE-01 JWT auth scope definition.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    /// <summary>Validates Windows credentials and returns a JWT + refresh token.</summary>
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // TODO: implement Windows credential validation against the Users table.
        return StatusCode(501, "Not implemented.");
    }

    /// <summary>Exchanges a refresh token for a new JWT.</summary>
    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] RefreshRequest request)
    {
        // TODO: implement refresh token validation.
        return StatusCode(501, "Not implemented.");
    }

    /// <summary>Revokes the caller's refresh token.</summary>
    [HttpPost("revoke")]
    [Authorize]
    public IActionResult Revoke()
    {
        // TODO: implement token revocation.
        return StatusCode(501, "Not implemented.");
    }
}

/// <summary>Request body for POST /api/auth/login.</summary>
public record LoginRequest(string WindowsUsername, string Password);

/// <summary>Request body for POST /api/auth/refresh.</summary>
public record RefreshRequest(string RefreshToken);
