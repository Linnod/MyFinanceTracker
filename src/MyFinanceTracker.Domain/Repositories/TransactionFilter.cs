using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Domain.Repositories;

public sealed record TransactionFilter(
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    IReadOnlyCollection<Category>? Categories = null,
    TransactionType? Type = null,
    int? Skip = null,
    int? Take = null)
{
    public override string ToString()
    {
        var parts = new List<string>();

        if (FromDate.HasValue)
        {
            parts.Add($"From: {FromDate.Value:dd.MM.yyyy HH:mm:ss}");
        }

        if (ToDate.HasValue)
        {
            parts.Add($"To: {ToDate.Value:dd.MM.yyyy HH:mm:ss}");
        }

        if (Categories is { Count: > 0 })
        {
            parts.Add($"Categories: [{string.Join(", ", Categories.Select(c => c.Name))}]");
        }

        if (Type.HasValue)
        {
            parts.Add($"Type: {Type.Value}");
        }

        if (Skip.HasValue)
        {
            parts.Add($"Skip: {Skip.Value}");
        }

        if (Take.HasValue)
        {
            parts.Add($"Take: {Take.Value}");
        }

        return parts.Count > 0
            ? string.Join(" | ", parts)
            : "Empty";
    }
}
