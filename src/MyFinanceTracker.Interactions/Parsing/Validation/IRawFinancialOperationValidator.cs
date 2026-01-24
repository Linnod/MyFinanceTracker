using MyFinanceTracker.Interactions.Parsing.Models;

namespace MyFinanceTracker.Interactions.Parsing.Validation;

internal interface IRawFinancialOperationValidator
{
    void Validate(RawFinancialOperation rawFinancialOperation);
}
