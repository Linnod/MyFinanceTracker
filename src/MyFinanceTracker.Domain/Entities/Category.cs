using MyFinanceTracker.Domain.Exceptions;

namespace MyFinanceTracker.Domain.Entities;

public record Category
{
    public string Id { get; }
    public string Name { get; }
    public IReadOnlyCollection<string> Aliases { get; }
    public bool IsIncome { get; }

    public Category(
        string id,
        string name,
        IReadOnlyCollection<string> aliases,
        bool isIncome = false)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new DomainException("Category ID cannot be empty or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Category Name cannot be empty or whitespace.");
        }

        if (aliases is null || aliases.Count == 0)
        {
            throw new DomainException("Category must have at least one alias.");
        }

        Id = id;
        Name = name;
        Aliases = aliases;
        IsIncome = isIncome;
    }

    public override string ToString() => Name;
}