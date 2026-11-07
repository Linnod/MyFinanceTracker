using MediatR;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.UseCases.Transaction.Update;

public sealed record UpdateTransactionRequest(
    Guid Id,
    TransactionType? Type = null,
    decimal? Amount = null,
    string? CategoryAlias = null,
    DateOnly? Date = null,
    string? Note = null,
    bool ClearNote = false) : IRequest<UpdateTransactionResponse>;
