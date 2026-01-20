using MyFinanceTracker.Interactions.Parsing.Models;

namespace MyFinanceTracker.Interactions.Parsing.Parser;

internal interface IFinancialOperationParser
{
    RawFinancialOperation Parse(string input);
}