using Google.GenAI.Types;
using MyFinanceTracker.Domain.Entities;
using Type = Google.GenAI.Types.Type;

namespace MyFinanceTracker.InputProcessing.Text.Gemini.Declarations;

internal sealed class GeminiToolDeclarationProvider : IGeminiToolDeclarationProvider
{
    private static readonly List<string> TransactionTypes = [.. Enum.GetNames<TransactionType>().Select(name => name.ToLowerInvariant())];

    private static readonly string TransactionTypesDescription = string.Join(" or ", TransactionTypes);

    private static readonly IReadOnlyList<Tool> Declarations = [
        new Tool
        {
            FunctionDeclarations =
            [
                new FunctionDeclaration
                {
                    Name = ToolNames.AddTransaction,
                    Description = $"Add one or more financial transactions ({TransactionTypesDescription}).",
                    Parameters = new Schema
                    {
                        Type = Type.Object,
                        Properties = new Dictionary<string, Schema>
                        {
                            ["type"] = new Schema
                            {
                                Type = Type.String,
                                Description = $"Transaction type: {TransactionTypesDescription}.",
                                Enum = TransactionTypes
                            },
                            ["amounts"] = new Schema
                            {
                                Type = Type.Array,
                                Description = "List of amounts spent or received (e.g. [150] or [100, 50.5]).",
                                Items = new Schema { Type = Type.Number }
                            },
                            ["categoryAlias"] = new Schema
                            {
                                Type = Type.String,
                                Description = "Category alias matching one from available categories list."
                            },
                            ["date"] = new Schema
                            {
                                Type = Type.String,
                                Description = "Date in YYYY-MM-DD format."
                            },
                            ["note"] = new Schema
                            {
                                Type = Type.String,
                                Description = "Optional note or comment describing the transaction."
                            },
                            ["recognizedInput"] = new Schema
                            {
                                Type = Type.String,
                                Description = "ALWAYS REQUIRED. Short human-readable summary of this specific action in user's language (e.g. 'Расход 100 € в категорию еда')."
                            }
                        },
                        Required = ["type", "amounts", "recognizedInput"]
                    }
                },
                new FunctionDeclaration
                {
                    Name = ToolNames.DeleteTransactions,
                    Description = "Delete or clear transactions for a category on a specific date.",
                    Parameters = new Schema
                    {
                        Type = Type.Object,
                        Properties = new Dictionary<string, Schema>
                        {
                            ["categoryAlias"] = new Schema
                            {
                                Type = Type.String,
                                Description = "Category alias to clear."
                            },
                            ["date"] = new Schema
                            {
                                Type = Type.String,
                                Description = "Specific date in YYYY-MM-DD format."
                            },
                            ["recognizedInput"] = new Schema
                            {
                                Type = Type.String,
                                Description = "ALWAYS REQUIRED. Short human-readable summary of this deletion action in user's language (e.g. 'Удаление транзакций из категории Еда за 11.08.2026')."
                            }
                        },
                        Required = ["categoryAlias", "date", "recognizedInput"]
                    }
                },
                new FunctionDeclaration
                {
                    Name = ToolNames.GetTransactions,
                    Description = "Get transactions for a category on a specific date.",
                    Parameters = new Schema
                    {
                        Type = Type.Object,
                        Properties = new Dictionary<string, Schema>
                        {
                            ["categoryAlias"] = new Schema
                            {
                                Type = Type.String,
                                Description = "Category alias to get transactions for."
                            },
                            ["date"] = new Schema
                            {
                                Type = Type.String,
                                Description = "Specific date in YYYY-MM-DD format."
                            },
                            ["recognizedInput"] = new Schema
                            {
                                Type = Type.String,
                                Description = "ALWAYS REQUIRED. Short human-readable summary of this action in user's language (e.g. 'Просмотр трат на еду за 11.08.2026')."
                            }
                        },
                        Required = ["recognizedInput"]
                    }
                },
                new FunctionDeclaration
                {
                    Name = ToolNames.ListCategories,
                    Description = "List all available categories and their aliases.",
                    Parameters = new Schema
                    {
                        Type = Type.Object,
                        Properties = new Dictionary<string, Schema>
                        {
                            ["recognizedInput"] = new Schema
                            {
                                Type = Type.String,
                                Description = "ALWAYS REQUIRED. Short human-readable summary of this action in user's language (e.g. 'Просмотр списка категорий')."
                            }
                        },
                        Required = ["recognizedInput"]
                    }
                }
            ]
        }
    ];

    public IReadOnlyList<Tool> GetToolDeclarations() => Declarations;

    public static class ToolNames
    {
        public const string AddTransaction = "add_transaction";
        public const string DeleteTransactions = "delete_transactions";
        public const string GetTransactions = "get_transactions";
        public const string ListCategories = "list_categories";

    }
}