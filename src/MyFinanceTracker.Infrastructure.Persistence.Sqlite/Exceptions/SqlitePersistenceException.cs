namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Exceptions;

internal sealed class SqlitePersistenceException : Exception
{
    public SqlitePersistenceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    public static SqlitePersistenceException CategoryNotFound(string categoryId) =>
        new($"Category with ID '{categoryId}' was not found.");

    public static SqlitePersistenceException TransactionNotFound(Guid transactionId) =>
        new($"Transaction with ID '{transactionId}' was not found.");
}
