using System.Collections.Concurrent;
using System.Text.Json;
using Banks.Contracts.Entities;
using Banks.Contracts.Enums;
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
        Func<
            InboundWebhookMessage,
            InboundTransactionWebhookOutboxMessageStatus,
            CancellationToken,
            Task<int>
        > UpdateMessageStatusDlg,
        CancellationToken cancelationToken = default
    )
    {
        var status = TransactionMessageProcessingStatus.Processed;
        var msgStatus = InboundTransactionWebhookOutboxMessageStatus.Succeeded;
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

                string bankCode =
                    Enum.GetName(message.BankCode)
                    ?? throw new ArgumentOutOfRangeException(
                        nameof(message.BankCode),
                        message.BankCode,
                        "Unknown bank code."
                    );

                var factory = parserResolver.ResolveFactory(bankCode);
                var parser = factory.CreateTransactionParser();

                var parsedTransactions = new ConcurrentBag<BankTransaction>();

                var failedRecords = new ConcurrentBag<TransacitonProcessingRecord>();

                Parallel.ForEach(
                    transactions,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = 4,
                        CancellationToken = cancelationToken,
                    },
                    t =>
                    {
                        try
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
                        catch (FormatException ex)
                        {
                            logger.LogWarning(
                                ex,
                                "Rejected transaction {TransactionId} in inbound message {MessageId}: {Reason}",
                                t.TransactionId,
                                message.Id,
                                ex.Message
                            );

                            failedRecords.Add(
                                new()
                                {
                                    TransactionId = t.TransactionId,
                                    Reason = ex.Message,
                                    ProcessedAtUtc = DateTime.UtcNow,
                                    Status = TransactionMessageProcessingStatus.Rejected,
                                }
                            );
                        }
                        catch (ArgumentException ex)
                        {
                            logger.LogWarning(
                                ex,
                                "Rejected transaction {TransactionId} in inbound message {MessageId}: {Reason}",
                                t.TransactionId,
                                message.Id,
                                ex.Message
                            );

                            failedRecords.Add(
                                new()
                                {
                                    TransactionId = t.TransactionId,
                                    Reason = ex.Message,
                                    ProcessedAtUtc = DateTime.UtcNow,
                                    Status = TransactionMessageProcessingStatus.Rejected,
                                }
                            );
                        }
                    }
                );

                if (!failedRecords.IsEmpty)
                {
                    try
                    {
                        await transactionRepository.WriteTransactionsProcessingRecordAsync(
                            failedRecords,
                            cancelationToken
                        );
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            ex,
                            "Failed to persist {Count} transaction processing records.",
                            failedRecords.Count
                        );
                        throw;
                    }
                }

                await stager.StageAsync(parsedTransactions, cancelationToken);
            }
        }
        catch (JsonException exception)
        {
            status = TransactionMessageProcessingStatus.Rejected;

            msgStatus = InboundTransactionWebhookOutboxMessageStatus.Failed;
            reason = $"Invalid JSON payload: {exception.Message}";
        }
        catch (FormatException ex)
        {
            status = TransactionMessageProcessingStatus.Rejected;

            msgStatus = InboundTransactionWebhookOutboxMessageStatus.Failed;
            reason = ex.Message;
        }
        catch (ArgumentException exception)
        {
            status = TransactionMessageProcessingStatus.Rejected;

            msgStatus = InboundTransactionWebhookOutboxMessageStatus.Failed;
            reason = exception.Message;
        }
        catch (OperationCanceledException) when (cancelationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            status = TransactionMessageProcessingStatus.Failed;

            msgStatus = InboundTransactionWebhookOutboxMessageStatus.Failed;
            reason = exception.Message;

            logger.LogError(
                exception,
                "Failed to process inbound message {MessageId}.",
                message.Id
            );
        }
        finally
        {
            var updated = await UpdateMessageStatusDlg.Invoke(message, msgStatus, cancelationToken);
            if (updated != 1)
            {
                logger.LogError(
                    "Expected to update one row for inbound message {MessageId}, but updated {UpdatedCount}.",
                    message.Id,
                    updated
                );

                throw new InvalidOperationException(
                    $"Failed to update status for inbound message {message.Id}."
                );
            }

            await transactionRepository.WriteMessageProcessingRecordAsync(
                message,
                status,
                reason,
                cancelationToken
            );

            await transactionRepository.SaveChangesAsync(cancelationToken);
        }
    }
}
