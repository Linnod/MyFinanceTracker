using DomainCategory = MyFinanceTracker.Domain.Entities.Category;

namespace MyFinanceTracker.UseCases.Common;

internal abstract record CategoryResolution
{
    private CategoryResolution() { }

    public sealed record Found(DomainCategory Category) : CategoryResolution;
    public sealed record NotFound(string? Suggestion) : CategoryResolution;
}
