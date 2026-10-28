using Banks.Contracts.Entities;
using Banks.Contracts.Enums;
using Transaction.Contracts.Entities;

namespace Transaction.Contracts.Interfaces;

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
