using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyFinanceTracker.Interactions.Api.Dtos;
using MyFinanceTracker.UseCases.Category.List;
using MyFinanceTracker.UseCases.Transaction.Create;
using MyFinanceTracker.UseCases.Transaction.Delete;

namespace MyFinanceTracker.Interactions.Api;

internal static class ApiInteractionEndpoints
{
    public static async Task<IResult> GetCategories(
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var response = await mediator.Send(new ListCategoriesRequest(), ct);

        return response switch
        {
            ListCategoriesResponse.Success success => Results.Ok(new
            {
                success.Categories.Count,
                Categories = success.Categories.Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.IsIncome,
                    c.Aliases
                })
            }),
            ListCategoriesResponse.Failure => Results.Problem("Domain service failure."),
            _ => throw new UnreachableException($"Unknown response type: {response.GetType()}")
        };
    }

    public static async Task<IResult> CreateTransaction(
        [FromBody] CreateTransactionDto dto,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var request = new CreateTransactionRequest(
            dto.Type,
            dto.Amounts,
            dto.CategoryAlias,
            dto.Date,
            dto.Note
        );

        var response = await mediator.Send(request, ct);

        return response switch
        {
            CreateTransactionResponse.Success success => Results.Ok(new
            {
                Message = $"{success.Amounts.Sum()} added to category '{success.CategoryName}'",
                success.CategoryName,
                success.Amounts,
                success.Date,
                success.Note
            }),

            CreateTransactionResponse.ValidationError validation => Results.BadRequest(new
            {
                Errors = validation.Errors.Select(e => new
                {
                    e.PropertyName,
                    e.Message,
                    e.Suggestion
                })
            }),

            CreateTransactionResponse.Failure => Results.Problem("Domain service failure."),
            _ => throw new UnreachableException($"Unknown response type: {response.GetType()}")
        };
    }

    public static async Task<IResult> DeleteTransactions(
        [FromBody] DeleteTransactionsDto dto,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var request = new DeleteTransactionsRequest(
            dto.CategoryAlias,
            dto.Date
        );

        var response = await mediator.Send(request, ct);

        return response switch
        {
            DeleteTransactionsResponse.Success success => Results.Ok(new
            {
                Message = $"Cleared category '{success.CategoryName}' for {success.Date:dd.MM.yyyy}",
                success.CategoryName,
                success.Date
            }),

            DeleteTransactionsResponse.ValidationError validation => Results.BadRequest(new
            {
                Errors = validation.Errors.Select(e => new
                {
                    e.PropertyName,
                    e.Message,
                    e.Suggestion
                })
            }),

            DeleteTransactionsResponse.Failure => Results.Problem("Domain service failure."),
            _ => throw new UnreachableException($"Unknown response type: {response.GetType()}")
        };
    }
}