namespace MyFinanceTracker.Interactions.Parsing.Validation.Exceptions;

internal class ValidationException : Exception
{
    private ValidationException(string message) : base(message) { }

    public static ValidationException NoAmountsFound() 
    {
        return new ValidationException("I couldn't find any amounts. Please specify the sum.");
    }

    public static ValidationException CategoryRequired(string operationType) 
    {
        return new ValidationException($"{operationType} requires a category.");
    }

    public static ValidationException IncomeShouldNotHaveCategory() 
    {
        return new ValidationException("Income transactions should not have a category assigned.");
    }
}