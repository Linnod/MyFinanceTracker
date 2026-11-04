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
                                Description = "ALWAYS REQUIRED. Short human-readable summary of this specific action in user's language (e.g. 'Expense 100 EUR in Food')."
                            }
                        },
                        Required = ["type", "amounts", "categoryAlias", "date", "recognizedInput"]
                    }
                },
                new FunctionDeclaration
                {
                    Name = ToolNames.DeleteTransactions,
                    Description = "Delete one or more financial transactions by their unique IDs (UUID/GUID).",
                    Parameters = new Schema
                    {
                        Type = Type.Object,
                        Properties = new Dictionary<string, Schema>
                        {
                            ["ids"] = new Schema
                            {
                                Type = Type.Array,
                                Description = "List of transaction IDs (UUID/GUID) to delete.",
                                Items = new Schema { Type = Type.String }
                            },
                            ["recognizedInput"] = new Schema
                            {
                                Type = Type.String,
                                Description = "ALWAYS REQUIRED. Short human-readable summary of this deletion action in user's language (e.g. 'Delete transactions [UUID]')."
                            }
                        },
                        Required = ["ids", "recognizedInput"]
                    }
                },
                new FunctionDeclaration
                {
                    Name = ToolNames.GetTransactions,
                    Description = "Get transactions for a date range, optionally filtered by categories and transaction type.",
                    Parameters = new Schema
                    {
                        Type = Type.Object,
                        Properties = new Dictionary<string, Schema>
                        {
                            ["fromDate"] = new Schema
                            {
                                Type = Type.String,
                                Description = "Start date in YYYY-MM-DD format (inclusive). If querying a single day, set equal to toDate."
                            },
                            ["toDate"] = new Schema
                            {
                                Type = Type.String,
                                Description = "End date in YYYY-MM-DD format (inclusive). If querying a single day, set equal to fromDate."
                            },
                            ["categoryAliases"] = new Schema
                            {
                                Type = Type.Array,
                                Description = "Optional list of category aliases to filter by. Omit to include all categories.",
                                Items = new Schema { Type = Type.String }
                            },
                            ["type"] = new Schema
                            {
                                Type = Type.String,
                                Description = $"Optional transaction type filter ({TransactionTypesDescription}). Omit to include all types.",
                                Enum = TransactionTypes
                            },
                            ["recognizedInput"] = new Schema
                            {
                                Type = Type.String,
                                Description = "ALWAYS REQUIRED. Short human-readable summary of this action in user's language (e.g. 'View expenses in Food for 2026-08-11')."
                            }
                        },
                        Required = ["fromDate", "toDate", "recognizedInput"]
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
                                Description = "ALWAYS REQUIRED. Short human-readable summary of this action in user's language (e.g. 'View category list')."
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
