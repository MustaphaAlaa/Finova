using Banks.Contracts.Entities;
using Banks.Contracts.Enums;
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
         Func<
             InboundWebhookMessage,
             InboundTransactionWebhookOutboxMessageStatus,
             CancellationToken,
             Task<int>
         > UpdateMessageStatusDlg,
         CancellationToken cancelationToken = default
     );
}
