using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MTM_Waitlist_Server.Api.Services;

namespace MTM_Waitlist_Server.Api.Controllers;

/// <summary>
/// CRUD endpoints for waitlist entries.
/// Implementation is a TODO stub pending FEATURE-01 scope definition.
/// </summary>
[ApiController]
[Route("api/waitlist")]
[Authorize]
public class WaitlistController : ControllerBase
{
    private readonly Service_ApiWaitlist _waitlistService;

    /// <summary>
    /// Initialises a new instance with the backing waitlist service.
    /// </summary>
    public WaitlistController(Service_ApiWaitlist waitlistService)
    {
        _waitlistService = waitlistService;
    }

    /// <summary>Returns all active waitlist entries.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _waitlistService.GetAllAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, $"Waitlist data is unavailable: {ex.Message}");
        }
    }

    /// <summary>Returns a single waitlist entry by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var entry = await _waitlistService.GetByIdAsync(id, cancellationToken);
            return entry is null ? NotFound() : Ok(entry);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, $"Waitlist data is unavailable: {ex.Message}");
        }
    }

    /// <summary>Creates a new waitlist entry.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Model_ApiWaitlistEntry request, CancellationToken cancellationToken)
    {
        if (!IsValid(request, requireWorkcenter: true))
        {
            return BadRequest("A valid waitlist entry payload is required.");
        }

        try
        {
            var created = await _waitlistService.CreateAsync(request, cancellationToken);
            return created is null
                ? StatusCode(StatusCodes.Status503ServiceUnavailable, "Waitlist entry could not be created.")
                : CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, $"Waitlist data is unavailable: {ex.Message}");
        }
    }

    /// <summary>Updates an existing waitlist entry.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Model_ApiWaitlistEntry request, CancellationToken cancellationToken)
    {
        if (!IsValid(request, requireWorkcenter: true))
        {
            return BadRequest("A valid waitlist entry payload is required.");
        }

        try
        {
            var existing = await _waitlistService.GetByIdAsync(id, cancellationToken);
            if (existing is null)
            {
                return NotFound();
            }

            request.Id = id;
            var updated = await _waitlistService.UpdateAsync(request, cancellationToken);
            return updated is null
                ? StatusCode(StatusCodes.Status503ServiceUnavailable, "Waitlist entry could not be updated.")
                : Ok(updated);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, $"Waitlist data is unavailable: {ex.Message}");
        }
    }

    /// <summary>Deletes a waitlist entry.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _waitlistService.GetByIdAsync(id, cancellationToken);
            if (existing is null)
            {
                return NotFound();
            }

            await _waitlistService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, $"Waitlist data is unavailable: {ex.Message}");
        }
    }

    private static bool IsValid(Model_ApiWaitlistEntry request, bool requireWorkcenter)
    {
        if (requireWorkcenter && string.IsNullOrWhiteSpace(request.WorkcenterName))
        {
            return false;
        }

        if (!Enum.IsDefined(request.RequestType))
        {
            return false;
        }

        if (!Enum.IsDefined(request.Status))
        {
            return false;
        }

        return request.Priority is >= 1 and <= 10;
    }
}
