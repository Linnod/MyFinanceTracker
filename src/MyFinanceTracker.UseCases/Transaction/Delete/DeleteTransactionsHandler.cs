using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;

namespace MyFinanceTracker.UseCases.Transaction.Delete;

internal sealed class DeleteTransactionsHandler(
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository,
    ILogger<DeleteTransactionsHandler> logger)
    : IRequestHandler<DeleteTransactionsRequest, DeleteTransactionsResponse>
{
    public async Task<DeleteTransactionsResponse> Handle(
        DeleteTransactionsRequest request,
        CancellationToken ct)
    {
        var finalDate = request.Date ?? DateOnly.FromDateTime(DateTime.Now);
        if (finalDate.Year < FinancialRules.MinAllowedYear || finalDate.Year > FinancialRules.MaxAllowedYear)
        {
            return new DeleteTransactionsResponse.ValidationError(
                $"Date {finalDate:dd.MM.yyyy} is out of allowed range ({FinancialRules.MinAllowedYear}-{FinancialRules.MaxAllowedYear})");
        }

        if (string.IsNullOrWhiteSpace(request.CategoryAlias))
        {
            return new DeleteTransactionsResponse.ValidationError("Category alias is required for deletion.");
        }

        logger.LogInformation("🚀 Processing deletion for Category: {Alias}, Date: {Date}",
            request.CategoryAlias, finalDate);

        var category = await categoryRepository.GetByAlias(request.CategoryAlias, ct);
        if (category is null)
        {
            logger.LogWarning("🔍 Category with alias '{Alias}' not found", request.CategoryAlias);
            
            return new DeleteTransactionsResponse.ValidationError($"Unknown category: {request.CategoryAlias}");
        }

        try
        {
            await transactionRepository.DeleteRange(category, finalDate, ct);

            logger.LogInformation("✅ Successfully cleared entries for {CatName} ({Date})",
                category.Name, finalDate);

            return new DeleteTransactionsResponse.Success(category.Name, finalDate);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Failed to delete transactions for {Alias}", request.CategoryAlias);

            return new DeleteTransactionsResponse.Failure("Failed to clear transactions. System error.");
        }
    }
}