using System.Net;
using BankSimulator.Domain;
using Microsoft.Extensions.Logging;

namespace BankSimulator.Application;

public class WebhookSubscriptionServices(
    ITransactionWebhookRepository webhookRepository,
    IWebhookSubscriptionValidation webhookSubscriptionValidation,
    ILogger<WebhookSubscriptionServices> logger
) : IWebhookSubscriptionServices
{
    public async Task<Result<string>> AddWebhookSubscription(
        CreateTransactionWebhookRequest webhookRequest
    )
    {
        try
        {
            var validationResult = webhookSubscriptionValidation.ValidateWebhookRequest(
                webhookRequest
            );

            if (!validationResult.IsSuccess)
                return validationResult;

            var webhookSubscription = new TransactionWebhookSubscription()
            {
                ClientId = webhookRequest.ClientId,
                CreatedOnUTC = DateTime.UtcNow,
                EventType = WebhookEventTypes.TransactionCreated,
                WebhookUrl = webhookRequest.WebhookUrl,
            };

            webhookRepository.AddWebhook(webhookSubscription);

            await webhookRepository.SaveChangesAsync();

            logger.LogInformation($"Webhook for client is created successfully");
            return Result<string>.Success(
                $"Webhook for client is created successfully" 
            );
        }
        catch (Exception ex)
        {
            logger.LogError($"Unexpected Exception, {ex.Message}");

            return Result<string>.Failure(ex.Message );
        }
    }
}
