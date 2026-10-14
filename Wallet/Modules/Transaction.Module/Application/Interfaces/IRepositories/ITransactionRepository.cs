// using BankSimulator.Domain;

// namespace BankSimulator.Application;

// public interface ITransactionRepository : IBaseRepository<Transaction>
// {
//     Task CreateTransactions(List<Transaction> transactions, CancellationToken cancellationToken);
//     Task CreateTransaction(Transaction transaction, CancellationToken cancellationToken);
//     Transaction GetTransactionsById(Guid transactionId, CancellationToken cancellationToken);

//     // Task<List<Transaction>> GetUnsentTransactions(
//     //     int page = 1,
//     //     int pageSize = 10,
//     //     CancellationToken cancellationToken = default
//     // );
//     Task<List<Transaction>> GetTransactionsByEventType(
//         string eventType,
//         bool sentTransaction = false,
//         bool allTransactions = false,
//         CancellationToken cancellationToken = default
//     );

//     Task<int> SaveChangesAsync();
// }
