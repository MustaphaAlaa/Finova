using System.ComponentModel.DataAnnotations;
using Banks.Application;
using Banks.Application.Dtos;
using Banks.Application.Interfaces.IServices;
using Banks.Domain.Enums;
using Banks.Presentation.Filters;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace Banks.Presentation.Controllers;

[ApiController]
[Route("api/bank-webhook")]
public sealed class BankController(IBankServices bankServices) : ControllerBase
{
    [HttpPost("{bankCode}/transaction")]
    [RequestSizeLimit((long)RequestSizeLimits.TransactionWebhook)]
    [RequestTimeout("Long")]
    [AuthorizeBankTransactionWebhook]
    public async Task<IActionResult> ReceiveTransactionNotificationAsync(
        BankTransactionWebhookRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await bankServices.WriteToOutboxAsync(request, cancellationToken);

            return Ok(Result<string>.Success("Transaction received"));
        }
        catch (ValidationException ex)
        {
            return BadRequest(Result<string>.Failure(ex.Message));
        }
    }
}