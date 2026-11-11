using MyFinanceTracker.Domain.Exceptions;

namespace MyFinanceTracker.Domain.Entities;

public record Category
{
    public string Id { get; }
    public string Name { get; }
    public bool IsArchived { get; }

    public Category(
        string id,
        string name,
        bool isArchived = false)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new DomainException("Category ID cannot be empty or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Category Name cannot be empty or whitespace.");
        }

        Id = id;
        Name = name;
        IsArchived = isArchived;
    }

    public override string ToString() => Name;
}
