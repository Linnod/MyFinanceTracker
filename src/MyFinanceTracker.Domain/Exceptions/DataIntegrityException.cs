namespace MyFinanceTracker.Domain.Exceptions;

public class DataIntegrityException : Exception
{
    public DataIntegrityException(string message) : base(message) { }

    public DataIntegrityException(string message, Exception innerException)
        : base(message, innerException) { }
}
