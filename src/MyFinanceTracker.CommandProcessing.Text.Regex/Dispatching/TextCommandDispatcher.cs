using MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands;
using MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands.AddTransaction;
using MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands.DeleteTransaction;
using MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands.ListCategories;

namespace MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching;

internal sealed class TextCommandDispatcher(
    ICommandHandler<AddTransactionCommand> addTransactionHandler,
    ICommandHandler<DeleteTransactionCommand> deleteTransactionHandler,
    ICommandHandler<ListCategoriesCommand> listCategoriesHandler) : ITextCommandDispatcher
{
    public Task<TextCommandResponse> Dispatch(ITextCommand command, CancellationToken ct = default)
    {
        return command switch
        {
            AddTransactionCommand c => addTransactionHandler.Handle(c, ct),
            DeleteTransactionCommand c => deleteTransactionHandler.Handle(c, ct),
            ListCategoriesCommand c => listCategoriesHandler.Handle(c, ct),
            _ => throw new ArgumentException($"Unsupported command type: {command.GetType().Name}")
        };
    }
}