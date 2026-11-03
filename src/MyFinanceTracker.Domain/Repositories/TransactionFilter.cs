using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Domain.Repositories;

public sealed record TransactionFilter(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    IReadOnlyCollection<Category>? Categories = null,
    TransactionType? Type = null)
{
    public override string ToString()
    {
        var parts = new List<string>();

        if (FromDate.HasValue)
        {
            parts.Add($"From: {FromDate.Value:dd.MM.yyyy}");
        }

        if (ToDate.HasValue)
        {
            parts.Add($"To: {ToDate.Value:dd.MM.yyyy}");
        }

        if (Categories is { Count: > 0 })
        {
            parts.Add($"Categories: [{string.Join(", ", Categories.Select(c => c.Name))}]");
        }

        if (Type.HasValue)
        {
            parts.Add($"Type: {Type.Value}");
        }

        return parts.Count > 0
            ? string.Join(" | ", parts)
            : "Empty";
    }
}
