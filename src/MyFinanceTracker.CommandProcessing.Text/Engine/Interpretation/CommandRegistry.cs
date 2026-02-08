namespace MyFinanceTracker.CommandProcessing.Text.Engine.Interpretation;

internal static class CommandRegistry
{
    public static readonly IReadOnlyCollection<TextCommandDomain> Domains =
    [
        new TextCommandDomain(
            Name: "transaction",
            Aliases: ["t", "tran", "transaction"],
            Actions: new Dictionary<string, TextCommandType>(StringComparer.OrdinalIgnoreCase)
            {
                { "add", TextCommandType.AddTransaction },
                { "new", TextCommandType.AddTransaction },
                { "+",   TextCommandType.AddTransaction },
                { "rem", TextCommandType.DeleteTransaction },
                { "del", TextCommandType.DeleteTransaction },
                { "-",   TextCommandType.DeleteTransaction },
            }
        ),

        new TextCommandDomain(
            Name: "category",
            Aliases: ["c", "cat", "category", "categories"],
            Actions: new Dictionary<string, TextCommandType>(StringComparer.OrdinalIgnoreCase)
            {
                { "all",  TextCommandType.ListCategories },
                { "list", TextCommandType.ListCategories },
            }
        )
    ];

    private static readonly Dictionary<string, TextCommandDomain> DomainLookup =
        Domains.SelectMany(d => d.Aliases.Select(a => new { Alias = a, Domain = d }))
               .ToDictionary(x => x.Alias, x => x.Domain, StringComparer.OrdinalIgnoreCase);

    public static bool TryGetDomain(string alias, out TextCommandDomain? domain)
        => DomainLookup.TryGetValue(alias, out domain);

    public static IEnumerable<string> AllDomainAliases => DomainLookup.Keys;

    public static string[] GetGeneralExamples() =>
        [.. Domains.Select(d => $"{d.Aliases.First()} ...")];

    public static IEnumerable<string> GetActionAliases(string domainName) =>
        Domains.First(d => d.Name == domainName).Actions.Keys.Where(k => k != string.Empty);
}
