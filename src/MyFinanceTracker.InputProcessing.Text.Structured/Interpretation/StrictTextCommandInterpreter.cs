using Microsoft.Extensions.Logging;
using MyFinanceTracker.Common.Utilities;

namespace MyFinanceTracker.InputProcessing.Text.Structured.Interpretation;

internal sealed partial class StrictTextCommandInterpreter(
    ICommandRegistry commandRegistry,
    ILogger<StrictTextCommandInterpreter> logger)
    : ITextCommandInterpreter
{
    public Task<InterpretationResult> Interpret(InterpretationInput input)
    {
        LogStarted(input);

        return Task.FromResult(InternalInterpret(input.Value));
    }

    private InterpretationResult InternalInterpret(string input)
    {
        var parts = input.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        var domainCandidate = parts[0];
        if (!commandRegistry.TryGetDomain(domainCandidate, out var domain))
        {
            var suggestion = FuzzyMatcher.GetClosest(domainCandidate, commandRegistry.AllDomainAliases);
            var unrecognizedResult = new InterpretationResult.Unrecognized(
                domainCandidate,
                ErrorCode.Syntax.InvalidFormat,
                suggestion,
                commandRegistry.GetGeneralExamples());

            LogUnrecognized(unrecognizedResult);
            return unrecognizedResult;
        }

        var actionCandidate = parts.Length > 1 ? parts[1] : string.Empty;
        if (!domain!.Actions.TryGetValue(actionCandidate, out var commandFactory))
        {
            var domainAliases = commandRegistry.GetActionAliases(domain.Name);
            var suggestion = FuzzyMatcher.GetClosest(actionCandidate, domainAliases);
            var examples = domain.Actions.Keys
                .Where(k => k != string.Empty)
                .Select(a => $"{domainCandidate} {a} ...")
                .ToArray();
            var unrecognizedResult = new InterpretationResult.Unrecognized(
                actionCandidate,
                ErrorCode.Syntax.InvalidFormat,
                suggestion,
                examples
            );
            LogUnrecognized(unrecognizedResult);

            return unrecognizedResult;
        }

        var payload = parts.Length > 2 ? parts[2].Trim() : string.Empty;
        var identifiedResult = new InterpretationResult.Identified(commandFactory(payload));
        LogIdentified(identifiedResult);

        return identifiedResult;
    }
}