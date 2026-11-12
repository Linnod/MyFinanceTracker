using MediatR;

namespace MyFinanceTracker.UseCases.Category.Create;

public sealed record CreateCategoryRequest(string Id, string Name) : IRequest<CreateCategoryResponse>
{
    public override string ToString() => $"Id: {Id}, Name: {Name}";
}
