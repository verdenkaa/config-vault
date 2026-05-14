using Microsoft.AspNetCore.Mvc;

namespace ConfigVault.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PingController : ControllerBase
{
    [HttpGet]
    public IActionResult Ping()
    {
        return Ok(new { message = "ConfigVault is running", timestamp = DateTime.UtcNow });
    }
}