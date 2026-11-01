using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Banks.Application.Dtos;
using Banks.Application.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banks.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/transfers")]
public sealed class TransferController(ISendMoneyService sendMoneyService) : ControllerBase
{
    [HttpPost("send")]
    [Produces("application/xml")]
    public async Task<IActionResult> SendMoneyAsync(
        SendMoneyRequest request,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        try
        {
            var xml = await sendMoneyService.CreatePaymentRequestXmlAsync(userId, request, cancellationToken);
            return Content(xml, "application/xml", System.Text.Encoding.UTF8);
        }
        catch (ValidationException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
