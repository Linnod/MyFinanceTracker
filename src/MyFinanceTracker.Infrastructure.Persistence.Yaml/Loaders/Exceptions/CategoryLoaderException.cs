namespace MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders.Exceptions;

internal sealed class CategoryLoaderException : Exception
{
    private CategoryLoaderException(string message, Exception? innerException = null)
        : base(message, innerException) { }

    public static CategoryLoaderException FileNotFound(string path)
    {
        return new CategoryLoaderException($"Category file not found at: {path}");
    }
}
