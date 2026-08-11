namespace MyFinanceTracker.InputProcessing.Text.Regex.Interpretation;

internal interface ICommandRegistry
{
    IReadOnlyCollection<TextCommandDomain> Domains { get; }
    bool TryGetDomain(string alias, out TextCommandDomain? domain);
    IEnumerable<string> AllDomainAliases { get; }
    string[] GetGeneralExamples();
    IEnumerable<string> GetActionAliases(string domainName);
}