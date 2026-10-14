// using BankSimulator.Application;
// using BankSimulator.Domain;

// namespace BankSimulator.Infrastructure.Repositories;
// public sealed class TransactionRepository(BankSimulatorDbContext dbContext)
//     : BaseRepository<Transaction>(dbContext),
//         ITransactionRepository
// {
//     public async Task CreateTransactions(
//         List<Transaction> transactions,
//         CancellationToken cancellationToken
//     ) => await dbContext.Transactions.AddRangeAsync(transactions, cancellationToken);

//     public async Task CreateTransaction(
//         Transaction transaction,
//         CancellationToken cancellationToken
//     ) => await dbContext.Transactions.AddAsync(transaction, cancellationToken);

//     public Task<List<Transaction>> GetTransactionsByEventType(
//         string eventType,
//         bool sentTransaction = false,
//         bool allTransactions = false,
//         CancellationToken cancellationToken = default
//     )
//     {
//         throw new NotImplementedException();
//     }

//     public Transaction GetTransactionsById(Guid transactionId, CancellationToken cancellationToken)
//     {
//         throw new NotImplementedException();
//     }

//     // public async Task<List<WebhookOutboxMessages>> GetUnsentTransactions(
//     //     int page = 1,
//     //     int pageSize = 10,
//     //     CancellationToken cancellationToken = default
//     // )
//     // {
//     //     var source = dbContext.WebhookOutboxMessages.Where(msg =>
//     //         msg.AttemptCount == 0 && msg.ProcessedOnUTC == null
//     //     );
//     //     var count = await source.CountAsync(cancellationToken);

//     //     var items = await source.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
//     //     return items;
//     // }

//     public async Task<int> SaveChangesAsync()
//     {
//         var res = await dbContext.SaveChangesAsync();
//         return res;
//     }
// }
