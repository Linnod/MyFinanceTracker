namespace MyFinanceTracker.CommandProcessing.Text.Engine.Interpretation;

internal interface ITextCommandInterpreter
{
    Task<InterpretationResult> Interpret(string input);
}