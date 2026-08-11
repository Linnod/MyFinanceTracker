namespace MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders.Exceptions;

internal class CategoryLoaderException : Exception
{
    private CategoryLoaderException(string message, Exception? innerException = null)
        : base(message, innerException) { }

    public static CategoryLoaderException FileNotFound(string path)
    {
        return new CategoryLoaderException($"Category file not found at: {path}");
    }

    public static CategoryLoaderException DuplicateId(string id)
    {
        return new CategoryLoaderException($"Duplicate Category ID found in YAML configuration: {id}");
    }

    public static CategoryLoaderException DuplicateAlias(string alias)
    {
        return new CategoryLoaderException($"Duplicate alias found in YAML configuration: {alias}");
    }

    public static CategoryLoaderException MissingAliases(string categoryId)
    {
        return new CategoryLoaderException($"Category with ID '{categoryId}' must have at least one alias in YAML configuration.");
    }

    public static CategoryLoaderException DeserializationFailed(string path, Exception inner)
    {
        return new CategoryLoaderException($"Failed to deserialize YAML category file at: {path}", inner);
    }

    public static CategoryLoaderException DefaultIncomeCategoryMissing(string alias)
    {
        return new CategoryLoaderException($"Required default income category with alias '{alias}' is missing in the YAML configuration.");
    }
}