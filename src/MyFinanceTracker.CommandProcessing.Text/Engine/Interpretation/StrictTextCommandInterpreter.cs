using Microsoft.Extensions.Logging;
using MyFinanceTracker.Common.Utilities;

namespace MyFinanceTracker.CommandProcessing.Text.Engine.Interpretation;

internal sealed partial class StrictTextCommandInterpreter(ILogger<StrictTextCommandInterpreter> logger)
    : ITextCommandInterpreter
{
    public Task<InterpretationResult> Interpret(string input)
    {
        LogInterpretationStarted(input);

        return Task.FromResult(InternalInterpret(input));
    }

    private static InterpretationResult InternalInterpret(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new InterpretationResult.EmptyInput();

        var parts = input.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        var domainCandidate = parts[0];
        if (!CommandRegistry.TryGetDomain(domainCandidate, out var domain))
        {
            var suggestion = FuzzyMatcher.GetClosest(domainCandidate, CommandRegistry.AllDomainAliases);
            return new InterpretationResult.Unrecognized(
                domainCandidate,
                suggestion,
                CommandRegistry.GetGeneralExamples());
        }

        var actionCandidate = parts.Length > 1 ? parts[1] : string.Empty;
        if (!domain!.Actions.TryGetValue(actionCandidate, out var type))
        {
            var domainAliases = CommandRegistry.GetActionAliases(domain.Name);
            var suggestion = FuzzyMatcher.GetClosest(actionCandidate, domainAliases);
            var examples = domain.Actions.Keys
                .Where(k => k != string.Empty)
                .Select(a => $"{domainCandidate} {a} ...")
                .ToArray();

            return new InterpretationResult.Unrecognized(actionCandidate, suggestion, examples);
        }

        var payload = parts.Length > 2 ? parts[2].Trim() : string.Empty;
        return new InterpretationResult.Identified(type, payload);
    }
}