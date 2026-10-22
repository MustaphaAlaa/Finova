using Banks.Application.Dtos;

namespace Banks.Application.Interfaces.IServices;

public interface IBankServices
{
    Task WriteToOutboxAsync(BankTransactionWebhookRequest request,
        CancellationToken ct);
}