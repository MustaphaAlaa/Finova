namespace BankSimulator.Application;

public class WebhookSubscriptionValidation : IWebhookSubscriptionValidation
{
    public Result<string> ValidateWebhookRequest(CreateTransactionWebhookRequest webhookRequest)
    {
        if (webhookRequest.ClientId == Guid.Empty)
            return Result<string>.Failure("Empty Client Id");

        if (string.IsNullOrWhiteSpace(webhookRequest.EventType))
            return Result<string>.Failure("Empty EventType");

        if (string.IsNullOrWhiteSpace(webhookRequest.WebhookUrl))
            return Result<string>.Failure("Empty Webhook Url");

        return Result<string>.Success("Valid Request");
    }
}
