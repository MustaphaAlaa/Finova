using Finova.Modules.Banks.Presentation.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Finova.Modules.Banks.Presentation.Controllers;

[ApiController]
[Route("api/banks")]
[AuthorizeBankTransactionWebhook]

public sealed class BankController : ControllerBase
{
    [HttpGet ]
    public IActionResult GetBank()
    {
        return Ok(new[] { "bank 1", "bank 2" });
    }
}
