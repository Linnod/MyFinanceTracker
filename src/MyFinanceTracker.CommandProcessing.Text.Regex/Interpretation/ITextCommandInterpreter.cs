namespace MyFinanceTracker.CommandProcessing.Text.Regex.Interpretation;

internal interface ITextCommandInterpreter
{
    Task<InterpretationResult> Interpret(string input);
}