using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MTM_Waitlist_Server.Api.Models.SetupTech;
using MTM_Waitlist_Server.Api.Services;

namespace MTM_Waitlist_Server.Api.Controllers;

/// <summary>
/// Endpoints that back the SetupTech workflow in the MAUI client.
/// </summary>
[ApiController]
[Route("api/setup-tech")]
[Authorize]
public sealed class SetupTechController : ControllerBase
{
    private readonly Service_ApiSetupTech _setupTechService;

    /// <summary>
    /// Initialises a new instance with the backing SetupTech service.
    /// </summary>
    public SetupTechController(Service_ApiSetupTech setupTechService)
    {
        _setupTechService = setupTechService;
    }

    /// <summary>
    /// Returns the current active job for one workcenter.
    /// </summary>
    [HttpGet("active-job/{workcenterId}")]
    public async Task<IActionResult> GetActiveJob(string workcenterId, CancellationToken cancellationToken)
    {
        var result = await _setupTechService.GetActiveJobAsync(workcenterId, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Data);
        }

        return result.ErrorMessage.Contains("No active job", StringComparison.OrdinalIgnoreCase)
            ? NotFound(result.ErrorMessage)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, result.ErrorMessage);
    }

    /// <summary>
    /// Saves the selected active job for a workcenter.
    /// </summary>
    [HttpPost("active-job")]
    public async Task<IActionResult> SetActiveJob([FromBody] Model_SetupTech_ActiveJob request, CancellationToken cancellationToken)
    {
        var result = await _setupTechService.SetActiveJobAsync(request, cancellationToken);
        return ToMutationResult(result);
    }

    /// <summary>
    /// Returns cached dunnage assignments for a work order and sequence pair.
    /// </summary>
    [HttpGet("dunnage-assignment/{workOrderId}/{sequenceNo:int}")]
    public async Task<IActionResult> GetDunnageAssignment(string workOrderId, int sequenceNo, CancellationToken cancellationToken)
    {
        var result = await _setupTechService.GetDunnageAssignmentAsync(workOrderId, sequenceNo, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Data)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, result.ErrorMessage);
    }

    /// <summary>
    /// Inserts or updates one cached dunnage assignment row.
    /// </summary>
    [HttpPut("dunnage-assignment")]
    public async Task<IActionResult> UpsertDunnageAssignment([FromBody] Model_SetupTech_DunnageAssignment request, CancellationToken cancellationToken)
    {
        var result = await _setupTechService.UpsertDunnageAssignmentAsync(request, cancellationToken);
        return ToMutationResult(result);
    }

    /// <summary>
    /// Deletes one cached dunnage assignment row.
    /// </summary>
    [HttpDelete("dunnage-assignment/{assignmentId:int}")]
    public async Task<IActionResult> DeleteDunnageAssignment(int assignmentId, CancellationToken cancellationToken)
    {
        var result = await _setupTechService.DeleteDunnageAssignmentAsync(assignmentId, cancellationToken);
        return ToMutationResult(result);
    }

    /// <summary>
    /// Returns the enabled dunnage type categories for the SetupTech picker.
    /// </summary>
    [HttpGet("dunnage-types")]
    public async Task<IActionResult> GetDunnageTypes(CancellationToken cancellationToken)
    {
        var result = await _setupTechService.GetEnabledDunnageTypesAsync(cancellationToken);
        return result.IsSuccess
            ? Ok(result.Data)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, result.ErrorMessage);
    }

    /// <summary>
    /// Returns archived job history for one workcenter.
    /// </summary>
    [HttpGet("job-history/{workcenterId}")]
    public async Task<IActionResult> GetJobHistory(string workcenterId, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var result = await _setupTechService.GetJobHistoryAsync(workcenterId, pageSize, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Data)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, result.ErrorMessage);
    }

    /// <summary>
    /// Returns cached subordinate parts for one work order and sequence pair.
    /// </summary>
    [HttpGet("subordinate-parts/{workOrderId}/{sequenceNo:int}")]
    public async Task<IActionResult> GetCachedSubordinateParts(string workOrderId, int sequenceNo, CancellationToken cancellationToken)
    {
        var result = await _setupTechService.GetCachedSubordinatePartsAsync(workOrderId, sequenceNo, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Data)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, result.ErrorMessage);
    }

    private IActionResult ToMutationResult(MTM_Waitlist_Server.Api.Models.Model_Dao_Result result)
    {
        if (result.IsSuccess)
        {
            return NoContent();
        }

        return result.ErrorMessage.Contains("required", StringComparison.OrdinalIgnoreCase)
            ? BadRequest(result.ErrorMessage)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, result.ErrorMessage);
    }
}