
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Interactions.Contracts;
using MyFinanceTracker.Interactions.Parsing.Parser;
using MyFinanceTracker.Interactions.Parsing.Validation;

namespace MyFinanceTracker.Interactions.Parsing;

internal class ParsingService(
    IRawFinancialOperationValidator validator, 
    IFinancialOperationParser parser)
{
    public FinancialOperation Process(string userInput)
    {
        var rawFinancialOperation = parser.Parse(userInput);
        validator.Validate(rawFinancialOperation);

        return new FinancialOperation(
            Type: rawFinancialOperation.Type ?? FinancialOperationType.Expense,
            CategoryAlias: rawFinancialOperation.CategoryAlias ?? "income",
            Amounts: rawFinancialOperation.Amounts,
            Date: rawFinancialOperation.Date ?? DateOnly.FromDateTime(DateTime.Today),
            Notes: rawFinancialOperation.Notes
        );
    }
}
