namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Exceptions;

internal sealed class SqlitePersistenceException : Exception
{
    public SqlitePersistenceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    public static SqlitePersistenceException CategoryNotFound(Guid transactionId, string categoryId) =>
        new($"Category '{categoryId}' for transaction '{transactionId}' could not be resolved.");

    public static SqlitePersistenceException TransactionNotFound(Guid transactionId) =>
        new($"Transaction with ID '{transactionId}' was not found.");
}
