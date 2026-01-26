namespace MyFinanceTracker.Interactions.Interpretation;

internal interface IInteractionInterpreter
{
    InterpretationResult Interpret(string input);
}