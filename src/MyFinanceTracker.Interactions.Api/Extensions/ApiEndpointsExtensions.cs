namespace MyFinanceTracker.Interactions.Api.Extensions;

internal static class ApiEndpointsExtensions
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/categories", ApiInteractionEndpoints.GetCategories)
           .WithName("GetCategories")
           .WithSummary("Get available categories and their aliases")
           .WithDescription("Call this BEFORE creating or deleting transactions to get valid category aliases.");

        api.MapPost("/transactions", ApiInteractionEndpoints.CreateTransaction)
           .WithName("CreateTransaction")
           .WithSummary("Create a new income or expense transaction")
           .WithDescription("Creates a transaction. Type can be 'Expense' or 'Income'. CategoryAlias must match an existing category alias.");

        api.MapDelete("/transactions", ApiInteractionEndpoints.DeleteTransactions)
           .WithName("DeleteTransactions")
           .WithSummary("Clear transactions for a category on a specific date")
           .WithDescription("Clears all transactions for the specified category alias and date (YYYY-MM-DD).");

        return app;
    }
}