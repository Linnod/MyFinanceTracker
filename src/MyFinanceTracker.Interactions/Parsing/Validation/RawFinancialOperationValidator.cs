using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Interactions.Parsing.Models;
using MyFinanceTracker.Interactions.Parsing.Validation.Exceptions;

namespace MyFinanceTracker.Interactions.Parsing.Validation;

internal class RawFinancialOperationValidator : IRawFinancialOperationValidator
{
    public void Validate(RawFinancialOperation rawFinancialOperation)
    {
        if (rawFinancialOperation.Amounts.Length == 0)
        {
            throw ValidationException.NoAmountsFound();
        }

        if (rawFinancialOperation.Type is FinancialOperationType.Expense or FinancialOperationType.Return or null)
        {
            if (string.IsNullOrWhiteSpace(rawFinancialOperation.CategoryAlias))
            {
                var typeName = rawFinancialOperation.Type?.ToString() ?? "Operation";
                
                throw ValidationException.CategoryRequired(typeName);
            }
        }

        if (rawFinancialOperation.Type == FinancialOperationType.Income)
        {
            if (!string.IsNullOrWhiteSpace(rawFinancialOperation.CategoryAlias))
            {
                throw ValidationException.IncomeShouldNotHaveCategory();
            }
        }
    }
}
