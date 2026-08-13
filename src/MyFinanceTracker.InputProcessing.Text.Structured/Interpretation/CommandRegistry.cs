using MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands;
using MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.AddTransaction;
using MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.DeleteTransaction;
using MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.ListCategories;

namespace MyFinanceTracker.InputProcessing.Text.Structured.Interpretation;

internal sealed class CommandRegistry : ICommandRegistry
{
    public IReadOnlyCollection<TextCommandDomain> Domains { get; }
    private readonly Dictionary<string, TextCommandDomain> domainLookup;

    public bool TryGetDomain(string alias, out TextCommandDomain? domain)
        => domainLookup.TryGetValue(alias, out domain);

    public IEnumerable<string> AllDomainAliases => domainLookup.Keys;

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
                new(["add", "new", "+"], typeof(AddTransactionCommand), p => new AddTransactionCommand(p)),
                new(["rem", "del", "-"], typeof(DeleteTransactionCommand), p => new DeleteTransactionCommand(p))
            ]
        ),

        new(
            Name: "category",
            Aliases: ["c", "cat", "category", "categories"],
            Actions:
            [
                new(["all", "list"], typeof(ListCategoriesCommand), _ => new ListCategoriesCommand())
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
                Actions: actionsMap,
                CommandTypes: [.. builder.Actions.Select(r => r.CommandType)]
            );
        })];

        domainLookup = Domains
            .SelectMany(d => d.Aliases.Select(a => new { Alias = a, Domain = d }))
            .ToDictionary(x => x.Alias, x => x.Domain, StringComparer.OrdinalIgnoreCase);
    }

    private record ActionRoute(string[] Aliases, Type CommandType, Func<string, ITextCommand> Factory);
    private record TextCommandDomainBuilder(string Name, string[] Aliases, List<ActionRoute> Actions);
}