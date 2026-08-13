namespace MyFinanceTracker.InputProcessing.Text.Structured.Interpretation;

internal interface ITextCommandInterpreter
{
    Task<InterpretationResult> Interpret(InterpretationInput input);
}