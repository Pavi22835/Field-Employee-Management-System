using FEMS.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FEMS.Api.Controllers;

[ApiController]
[Route("api/health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<object>> Get() =>
        Ok(ApiResponse<object>.Ok(new { status = "Healthy", utcTime = DateTimeOffset.UtcNow }));
}
