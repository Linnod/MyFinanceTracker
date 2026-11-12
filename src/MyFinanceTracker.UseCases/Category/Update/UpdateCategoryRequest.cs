using MediatR;

namespace MyFinanceTracker.UseCases.Category.Update;

public sealed record UpdateCategoryRequest(
    string Id,
    string? Name = null,
    bool? IsArchived = null) : IRequest<UpdateCategoryResponse>
{
    public override string ToString()
    {
        var parts = new List<string> { $"Id: {Id}" };

        if (Name is not null)
        {
            parts.Add($"Name: {Name}");
        }

        if (IsArchived.HasValue)
        {
            parts.Add($"IsArchived: {IsArchived.Value}");
        }

        return string.Join(" | ", parts);
    }
}
