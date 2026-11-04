using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Askyl.Dsm.WebHosting.Ui.Controllers;

/// <summary>
/// The DSM connection diagnostic. No [AuthorizeSession]: it is shown precisely when nobody can sign in.
/// It takes no input, so a request cannot point the checks anywhere but the configured DSM.
/// </summary>
[ApiController]
[Route(DsmDiagnosticRoutes.ControllerBaseRoute)]
public class DsmDiagnosticController(IDsmConnectionDiagnosticService diagnosticService) : ControllerBase
{
    /// <summary>
    /// Returns the latest report; only the first call after startup runs the checks.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DsmConnectionReportResult>> GetReportAsync(CancellationToken cancellationToken)
        => Ok(await diagnosticService.GetReportAsync(cancellationToken));

    /// <summary>
    /// Re-reads the DSM settings and runs the checks again.
    /// </summary>
    [HttpPost(DsmDiagnosticRoutes.RecheckRoute)]
    [EnableRateLimiting(ApplicationConstants.RateLimitPolicyDiagnostic)]
    public async Task<ActionResult<DsmConnectionReportResult>> RecheckAsync(CancellationToken cancellationToken)
        => Ok(await diagnosticService.RecheckAsync(cancellationToken));
}
