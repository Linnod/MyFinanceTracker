using MediatR;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;

namespace MyFinanceTracker.UseCases.Transaction.Update;

internal sealed class UpdateTransactionHandler(
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository)
    : IRequestHandler<UpdateTransactionRequest, UpdateTransactionResponse>
{
    public async Task<UpdateTransactionResponse> Handle(
        UpdateTransactionRequest request,
        CancellationToken ct)
    {
        var existing = await transactionRepository.GetById(request.Id, ct);
        if (existing is null)
        {
            return new UpdateTransactionResponse.ValidationError([
                new ValidationErrorItem(ValidationErrorCode.Transaction.TransactionNotFound, request.Id.ToString())
            ]);
        }

        var (category, error) = await ResolveCategory(request.CategoryAlias, existing.Category, ct);
        if (error != null)
        {
            return error;
        }

        var note = ResolveNote(request, existing.Note);
        var updated = new Domain.Entities.Transaction(
            existing.Id,
            request.Type ?? existing.Type,
            category!,
            request.Amount ?? existing.Amount,
            request.Date ?? existing.Date,
            note);

        await transactionRepository.Update(updated, ct);

        return new UpdateTransactionResponse.Success(updated);
    }

    private async Task<(Domain.Entities.Category? Category, UpdateTransactionResponse? Error)> ResolveCategory(
        string? categoryAlias,
        Domain.Entities.Category existingCategory,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(categoryAlias))
        {
            return (existingCategory, null);
        }

        var resolved = await categoryRepository.Resolve(categoryAlias, ct);
        if (resolved is null)
        {
            return (null, new UpdateTransactionResponse.ValidationError([
                new ValidationErrorItem(ValidationErrorCode.Transaction.CategoryNotFound, categoryAlias)
            ]));
        }

        if (resolved.IsArchived)
        {
            return (null, new UpdateTransactionResponse.ValidationError([
                new ValidationErrorItem(ValidationErrorCode.Transaction.CategoryArchived, categoryAlias)
            ]));
        }

        return (resolved, null);
    }

    private static string? ResolveNote(UpdateTransactionRequest request, string? existingNote)
    {
        if (request.ClearNote)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            return request.Note;
        }

        return existingNote;
    }
}
