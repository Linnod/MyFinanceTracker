using MyFinanceTracker.Domain.Exceptions;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml.Parsers.Exceptions;

internal sealed class CategoryParserException : Exception
{
    private CategoryParserException(string message, Exception? innerException = null)
        : base(message, innerException) { }

    public static CategoryParserException EmptyContent()
    {
        return new CategoryParserException("Category content is empty.");
    }

    public static CategoryParserException NoCategoriesFound()
    {
        return new CategoryParserException("Category content contains no categories.");
    }

    public static CategoryParserException DuplicateId(string id)
    {
        return new CategoryParserException($"Duplicate Category ID: '{id}'");
    }

    public static CategoryParserException DuplicateName(string name)
    {
        return new CategoryParserException($"Duplicate Category Name: '{name}'");
    }

    public static CategoryParserException InvalidCategory(DomainException inner)
    {
        return new CategoryParserException($"Invalid category: {inner.Message}", inner);
    }

    public static CategoryParserException DeserializationFailed(Exception inner)
    {
        return new CategoryParserException("Failed to deserialize category file.", inner);
    }
}
