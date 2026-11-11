using Google.GenAI.Types;
using MyFinanceTracker.Domain.Entities;
using Type = Google.GenAI.Types.Type;

namespace MyFinanceTracker.InputProcessing.Gemini.Declarations;

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
                            ["categoryId"] = new Schema
                            {
                                Type = Type.String,
                                Description = "Category ID matching one from available categories list."
                            },
                            ["date"] = new Schema
                            {
                                Type = Type.String,
                                Description = "Date and time in ISO 8601 format (YYYY-MM-DDTHH:mm:ss)."
                            },
                            ["note"] = new Schema
                            {
                                Type = Type.String,
                                Description = "Optional note or comment describing the transaction."
                            }
                        },
                        Required = ["type", "amounts", "categoryId", "date"]
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
                            }
                        },
                        Required = ["ids"]
                    }
                },
                new FunctionDeclaration
                {
                    Name = ToolNames.UpdateTransaction,
                    Description = "Update an existing financial transaction by its unique ID (UUID/GUID). Only provided fields will be updated.",
                    Parameters = new Schema
                    {
                        Type = Type.Object,
                        Properties = new Dictionary<string, Schema>
                        {
                            ["id"] = new Schema
                            {
                                Type = Type.String,
                                Description = "Unique transaction ID (UUID/GUID) to update."
                            },
                            ["type"] = new Schema
                            {
                                Type = Type.String,
                                Description = $"Optional new transaction type: {TransactionTypesDescription}.",
                                Enum = TransactionTypes
                            },
                            ["amount"] = new Schema
                            {
                                Type = Type.Number,
                                Description = "Optional new positive amount."
                            },
                            ["categoryId"] = new Schema
                            {
                                Type = Type.String,
                                Description = "Optional new category ID."
                            },
                            ["date"] = new Schema
                            {
                                Type = Type.String,
                                Description = "Optional new date and time in ISO 8601 format (YYYY-MM-DDTHH:mm:ss)."
                            },
                            ["note"] = new Schema
                            {
                                Type = Type.String,
                                Description = "Optional new note or comment describing the transaction."
                            },
                            ["clearNote"] = new Schema
                            {
                                Type = Type.Boolean,
                                Description = "Set to true ONLY if user explicitly requested to remove/clear the existing note or comment."
                            }
                        },
                        Required = ["id"]
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
                                Description = "Start date and time in ISO 8601 format (YYYY-MM-DDTHH:mm:ss, inclusive). For a full day, use YYYY-MM-DDT00:00:00."
                            },
                            ["toDate"] = new Schema
                            {
                                Type = Type.String,
                                Description = "End date and time in ISO 8601 format (YYYY-MM-DDTHH:mm:ss, inclusive). For a full day, use YYYY-MM-DDT23:59:59."
                            },
                            ["categoryIds"] = new Schema
                            {
                                Type = Type.Array,
                                Description = "Optional list of category IDs to filter by. Omit to include all categories.",
                                Items = new Schema { Type = Type.String }
                            },
                            ["type"] = new Schema
                            {
                                Type = Type.String,
                                Description = $"Optional transaction type filter ({TransactionTypesDescription}). Omit to include all types.",
                                Enum = TransactionTypes
                            },
                            ["limit"] = new Schema
                            {
                                Type = Type.Integer,
                                Description = "Optional maximum number of recent transactions to return (e.g. for queries like 'show last 5 expenses' or 'top 10 expenses')."
                            }
                        },
                        Required = ["fromDate", "toDate"]
                    }
                },
                new FunctionDeclaration
                {
                    Name = ToolNames.ListCategories,
                    Description = "List all available categories."
                }
            ]
        }
    ];

    public IReadOnlyList<Tool> GetToolDeclarations() => Declarations;

    public static class ToolNames
    {
        public const string AddTransaction = "add_transaction";
        public const string DeleteTransactions = "delete_transactions";
        public const string UpdateTransaction = "update_transaction";
        public const string GetTransactions = "get_transactions";
        public const string ListCategories = "list_categories";
    }
}
