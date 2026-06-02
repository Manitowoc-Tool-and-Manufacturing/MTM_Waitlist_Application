using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MTM_Waitlist_Server.Api.Data;

namespace MTM_Waitlist_Server.Api.Controllers;

/// <summary>
/// Read-only endpoints that proxy the Infor Visual data needed by Feature 07.
/// </summary>
[ApiController]
[Route("api/infor-visual")]
[Authorize]
public sealed class InforVisualController : ControllerBase
{
    private readonly Dao_InforVisualWorkOrder _inforVisualDao;

    /// <summary>
    /// Initialises a new instance with the backing Infor Visual DAO.
    /// </summary>
    public InforVisualController(Dao_InforVisualWorkOrder inforVisualDao)
    {
        _inforVisualDao = inforVisualDao;
    }

    /// <summary>
    /// Returns all schedulable workcenters.
    /// </summary>
    [HttpGet("workcenters")]
    public async Task<IActionResult> GetWorkcenters(CancellationToken cancellationToken)
    {
        var result = await _inforVisualDao.GetAllWorkcentersAsync(cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Returns one work order header.
    /// </summary>
    [HttpGet("work-orders/{workOrderId}")]
    public async Task<IActionResult> GetWorkOrderHeader(string workOrderId, CancellationToken cancellationToken)
    {
        var result = await _inforVisualDao.GetWorkOrderHeaderAsync(workOrderId, cancellationToken);
        return ToActionResult(result, notFoundMessage: $"Work order '{workOrderId}' was not found in Infor Visual.");
    }

    /// <summary>
    /// Returns all sequences for one work order.
    /// </summary>
    [HttpGet("work-orders/{workOrderId}/sequences")]
    public async Task<IActionResult> GetWorkOrderSequences(string workOrderId, CancellationToken cancellationToken)
    {
        var result = await _inforVisualDao.GetWorkOrderSequencesAsync(workOrderId, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Returns subordinate parts for one work order sequence.
    /// </summary>
    [HttpGet("work-orders/{workOrderId}/sequences/{sequenceNo:int}/subordinate-parts")]
    public async Task<IActionResult> GetSubordinateParts(string workOrderId, int sequenceNo, CancellationToken cancellationToken)
    {
        var result = await _inforVisualDao.GetSubordinatePartsAsync(workOrderId, sequenceNo, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Returns active work orders for one workcenter resource.
    /// </summary>
    [HttpGet("resources/{resourceId}/active-work-orders")]
    public async Task<IActionResult> GetActiveWorkOrders(string resourceId, CancellationToken cancellationToken)
    {
        var result = await _inforVisualDao.GetActiveWorkOrdersForResourceAsync(resourceId, cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(MTM_Waitlist_Server.Api.Models.Model_Dao_Result<T> result, string? notFoundMessage = null)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Data);
        }

        if (!string.IsNullOrWhiteSpace(notFoundMessage)
            && string.Equals(result.ErrorMessage, notFoundMessage, StringComparison.Ordinal))
        {
            return NotFound(result.ErrorMessage);
        }

        if (result.ErrorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(result.ErrorMessage);
        }

        return StatusCode(StatusCodes.Status503ServiceUnavailable, result.ErrorMessage);
    }
}