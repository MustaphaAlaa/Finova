using Banks.Contracts.Entities;
using Transactions.Contracts.Entities;

namespace Transactions.Contracts.Interfaces;

public interface ITransactionServies
{
    BankTransaction CreateBankTransaction(
        InboundWebhookMessage webhookMessage,
        TransactionsRequest transactionRequest
    );

    Task ProcessInboundTransactionMessageAsync(
        InboundWebhookMessage message,
        CancellationToken cancellationToken = default
    );
}
