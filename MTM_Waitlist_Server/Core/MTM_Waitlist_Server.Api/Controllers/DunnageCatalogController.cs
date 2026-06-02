using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MTM_Waitlist_Server.Api.Services;

namespace MTM_Waitlist_Server.Api.Controllers;

/// <summary>
/// Exposes the cross-database dunnage catalog used by the SetupTech flow.
/// </summary>
[ApiController]
[Route("api/dunnage-catalog")]
[Authorize]
public sealed class DunnageCatalogController : ControllerBase
{
    private readonly Service_ApiSetupTech _setupTechService;

    /// <summary>
    /// Initialises a new instance with the backing SetupTech service.
    /// </summary>
    public DunnageCatalogController(Service_ApiSetupTech setupTechService)
    {
        _setupTechService = setupTechService;
    }

    /// <summary>
    /// Returns the full dunnage catalog from the receiving application database.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
    {
        var result = await _setupTechService.GetDunnageCatalogAsync(cancellationToken);
        return result.IsSuccess
            ? Ok(result.Data)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, result.ErrorMessage);
    }
}