using System.Collections.Concurrent;
using System.Text.Json;
using Banks.Contracts.Entities;
using Microsoft.Extensions.Logging;
using Transaction.Contracts.Enums;
using Transactions.Application.Services.BankTransactionParserFactory;
using Transactions.Contracts.Entities;
using Transactions.Contracts.Interfaces;
using Transactions.Infrastructe;

namespace Transaction.Application.Services;

public class TransactionServices(
    IBankTransactionStager stager,
    ITransactionRepository transactionRepository,
    BankTransactionParserResolver parserResolver,
    ITransactionServicesValidator transactionServicesValidator,
    ILogger<TransactionServices> logger
) : ITransactionServies
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public BankTransaction CreateBankTransaction(
        InboundWebhookMessage webhookMessage,
        TransactionsRequest transactionRequest
    )
    {
        var transactionFactory = parserResolver.ResolveFactory(nameof(webhookMessage.BankCode));

        var parser = transactionFactory.CreateTransactionParser();

        var bankTransaction = parser.Parse(transactionRequest.Transaction);

        bankTransaction.WebhookDeliveryId = transactionRequest.WebhookDeliveryId;
        bankTransaction.Destination = transactionRequest.Destination;
        bankTransaction.Source = transactionRequest.Source;
        bankTransaction.BankId = webhookMessage.BankId;

        return bankTransaction;
    }

    public async Task ProcessInboundTransactionMessageAsync(
        InboundWebhookMessage message,
        CancellationToken cancelationToken = default
    )
    {
        var status = TransactionMessageProcessingStatus.Processed;
        string? reason = null;

        try
        {
            var transactionRequests = message.Payload.RootElement.Deserialize<
                List<TransactionsRequest>
            >(JsonOptions);

            if (transactionRequests is null)
            {
                status = TransactionMessageProcessingStatus.Rejected;
                reason = "Payload deserialized to null.";
            }
            else
            {
                var transactions =
                    await transactionServicesValidator.ValidateTransactionRequestsAsync(
                        transactionRequests,
                        cancelationToken
                    );

                string bankCode = Enum.GetName(message.BankCode)
                    ?? throw new ArgumentOutOfRangeException(
                        nameof(message.BankCode),
                        message.BankCode,
                        "Unknown bank code."
                    );

                var factory = parserResolver.ResolveFactory(bankCode);
                var parser = factory.CreateTransactionParser();

                var parsedTransactions = new ConcurrentBag<BankTransaction>();

                Parallel.ForEach(
                    transactions,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = 4,
                        CancellationToken = cancelationToken,
                    },
                    t =>
                    {
                        var bankTransaction = parser.Parse(t.Transaction);
                        bankTransaction.Id = Guid.NewGuid();
                        bankTransaction.TransactionId = t.TransactionId;
                        bankTransaction.WebhookDeliveryId = message.WebhookDeliveryId;
                        bankTransaction.BankId = message.BankId;

                        bankTransaction.Currency = Enum.Parse<Currencies>(t.Currency);
                        bankTransaction.Source = t.Source;
                        bankTransaction.Destination = t.Destination;
                        parsedTransactions.Add(bankTransaction);
                    }
                );

                await stager.StageAsync(parsedTransactions, cancelationToken);
            }
        }
        catch (JsonException exception)
        {
            status = TransactionMessageProcessingStatus.Rejected;
            reason = $"Invalid JSON payload: {exception.Message}";
        }
        catch (ArgumentException exception)
        {
            status = TransactionMessageProcessingStatus.Rejected;
            reason = exception.Message;
        }
        catch (OperationCanceledException) when (cancelationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            status = TransactionMessageProcessingStatus.Failed;
            reason = exception.Message;

            logger.LogError(
                exception,
                "Failed to process inbound message {MessageId}.",
                message.Id
            );
        }
        finally
        {
            await transactionRepository.WriteMessageProcessingRecordAsync(
                message,
                status,
                reason,
                cancelationToken
            );
        }
    }
}
