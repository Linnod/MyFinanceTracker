namespace MyFinanceTracker.Domain.Entities;

public record Category(
    string Id,
    string Name,
    IReadOnlyCollection<string> Aliases,
    bool IsIncome = false
)
{
    public override string ToString() => Name;
}