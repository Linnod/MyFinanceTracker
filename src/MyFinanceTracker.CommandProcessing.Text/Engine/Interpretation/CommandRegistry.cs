using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands;
using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.AddTransaction;
using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.DeleteTransaction;
using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.ListCategories;

namespace MyFinanceTracker.CommandProcessing.Text.Engine.Interpretation;

internal sealed class CommandRegistry : ICommandRegistry
{
    public IReadOnlyCollection<TextCommandDomain> Domains { get; }
    private readonly Dictionary<string, TextCommandDomain> _domainLookup;

    public bool TryGetDomain(string alias, out TextCommandDomain? domain)
        => _domainLookup.TryGetValue(alias, out domain);

    public IEnumerable<string> AllDomainAliases => _domainLookup.Keys;

    public string[] GetGeneralExamples() =>
        [.. Domains.Select(d => $"{d.Aliases.First()} ...")];

    public IEnumerable<string> GetActionAliases(string domainName) =>
        Domains.First(d => d.Name == domainName).Actions.Keys.Where(k => k != string.Empty);

    private static readonly List<TextCommandDomainBuilder> RouteMap =
    [
        new(
            Name: "transaction",
            Aliases: ["t", "tran", "transaction"],
            Actions:
            [
                new(["add", "new", "+"], p => new AddTransactionCommand(p)),
                new(["rem", "del", "-"], p => new DeleteTransactionCommand(p))
            ]
        ),

        new(
            Name: "category",
            Aliases: ["c", "cat", "category", "categories"],
            Actions:
            [
                new(["all", "list"], _ => new ListCategoriesCommand())
            ]
        )
    ];

    public CommandRegistry()
    {
        Domains = [.. RouteMap.Select(builder => 
        {
            var actionsMap = new Dictionary<string, Func<string, ITextCommand>>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var route in builder.Actions)
            {
                foreach (var actionAlias in route.Aliases)
                {
                    actionsMap[actionAlias] = route.Factory;
                }
            }

            return new TextCommandDomain(
                Name: builder.Name,
                Aliases: builder.Aliases,
                Actions: actionsMap
            );
        })];

        _domainLookup = Domains
            .SelectMany(d => d.Aliases.Select(a => new { Alias = a, Domain = d }))
            .ToDictionary(x => x.Alias, x => x.Domain, StringComparer.OrdinalIgnoreCase);
    }

    private record ActionRoute(string[] Aliases, Func<string, ITextCommand> Factory);
    private record TextCommandDomainBuilder(string Name, string[] Aliases, List<ActionRoute> Actions);
}