using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MTM_Waitlist_Server.Api.Services;

namespace MTM_Waitlist_Server.Api.Controllers;

/// <summary>
/// Authentication endpoints for MAUI client login, token refresh, logout,
/// workstation detection, and Windows auto-login.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly Service_ApiAuth _authService;

    /// <summary>
    /// Initialises a new instance with the backing authentication service.
    /// </summary>
    public AuthController(Service_ApiAuth authService)
    {
        _authService = authService;
    }

    /// <summary>Validates Windows credentials and returns a JWT + refresh token.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Username and password are required.");
        }

        try
        {
            var result = await _authService.LoginAsync(request.Username, request.Password, cancellationToken);
            return result is null
                ? Unauthorized("Invalid username or password.")
                : Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>Exchanges a refresh token for a new JWT.</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest("Refresh token is required.");
        }

        try
        {
            var result = await _authService.RefreshAsync(request.RefreshToken, cancellationToken);
            return result is null
                ? Unauthorized("Refresh token is invalid or expired.")
                : Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>Revokes a refresh token on logout.</summary>
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest("Refresh token is required.");
        }

        try
        {
            await _authService.RevokeAsync(request.RefreshToken, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>Checks whether the supplied Windows username belongs to a shared workstation.</summary>
    [HttpPost("check-workstation")]
    public async Task<IActionResult> CheckWorkstation([FromBody] CheckWorkstationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.WindowsUsername))
        {
            return BadRequest("Windows username is required.");
        }

        try
        {
            return Ok(await _authService.CheckWorkstationAsync(request.WindowsUsername, cancellationToken));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>Attempts silent Windows auto-login for a personal workstation.</summary>
    [HttpPost("auto-login")]
    public async Task<IActionResult> AutoLogin([FromBody] AutoLoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.WindowsUsername))
        {
            return BadRequest("Windows username is required.");
        }

        try
        {
            var result = await _authService.AutoLoginAsync(request.WindowsUsername, cancellationToken);
            return result is null
                ? Unauthorized("No active user is mapped to that Windows identity.")
                : Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message);
        }
    }
}

/// <summary>Request body for POST /api/auth/login.</summary>
public record LoginRequest(string Username, string Password);

/// <summary>Request body for POST /api/auth/refresh.</summary>
public record RefreshRequest(string RefreshToken);

/// <summary>Request body for POST /api/auth/check-workstation.</summary>
public record CheckWorkstationRequest(string WindowsUsername);

/// <summary>Request body for POST /api/auth/auto-login.</summary>
public record AutoLoginRequest(string WindowsUsername);
