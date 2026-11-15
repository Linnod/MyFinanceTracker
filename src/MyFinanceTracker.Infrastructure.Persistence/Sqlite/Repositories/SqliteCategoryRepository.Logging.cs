using Microsoft.Extensions.Logging;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Logging;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Repositories;

using static LogEvents.CategoryRepository;

internal partial class SqliteCategoryRepository
{
    [LoggerMessage(
        EventId = GettingAllCategories,
        Level = LogLevel.Debug,
        Message = "Fetching all categories from SQLite")]
    partial void LogGettingAllCategories();

    [LoggerMessage(
        EventId = GettingCategoryById,
        Level = LogLevel.Debug,
        Message = "Fetching category with ID '{Id}' from SQLite")]
    partial void LogGettingCategoryById(string id);

    [LoggerMessage(
        EventId = GettingCategoriesByIds,
        Level = LogLevel.Debug,
        Message = "Fetching {Count} categories by IDs from SQLite")]
    partial void LogGettingCategoriesByIds(int count);

    [LoggerMessage(
        EventId = AddingCategory,
        Level = LogLevel.Information,
        Message = "Adding category '{Id}' to SQLite and outbox")]
    partial void LogAddingCategory(string id);

    [LoggerMessage(
        EventId = UpdatingCategory,
        Level = LogLevel.Information,
        Message = "Updating category '{Id}' in SQLite and outbox")]
    partial void LogUpdatingCategory(string id);
}
