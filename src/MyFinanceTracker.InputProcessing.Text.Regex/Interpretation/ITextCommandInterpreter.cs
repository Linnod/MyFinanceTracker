namespace MyFinanceTracker.InputProcessing.Text.Regex.Interpretation;

internal interface ITextCommandInterpreter
{
    Task<InterpretationResult> Interpret(InterpretationInput input);
}