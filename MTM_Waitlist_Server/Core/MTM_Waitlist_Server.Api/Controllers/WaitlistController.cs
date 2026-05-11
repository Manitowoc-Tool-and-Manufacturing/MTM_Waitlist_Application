using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    /// <summary>Returns all active waitlist entries.</summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        // TODO: implement via IService_WaitlistEntry.
        return StatusCode(501, "Not implemented.");
    }

    /// <summary>Returns a single waitlist entry by ID.</summary>
    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        return StatusCode(501, "Not implemented.");
    }

    /// <summary>Creates a new waitlist entry.</summary>
    [HttpPost]
    public IActionResult Create([FromBody] object request)
    {
        return StatusCode(501, "Not implemented.");
    }

    /// <summary>Updates an existing waitlist entry.</summary>
    [HttpPut("{id:int}")]
    public IActionResult Update(int id, [FromBody] object request)
    {
        return StatusCode(501, "Not implemented.");
    }

    /// <summary>Deletes a waitlist entry.</summary>
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        return StatusCode(501, "Not implemented.");
    }
}
