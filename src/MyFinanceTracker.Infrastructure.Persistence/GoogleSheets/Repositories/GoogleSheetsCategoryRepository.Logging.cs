using Microsoft.Extensions.Logging;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Logging;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Repositories;

using static LogEvents.CategoryRepository;

internal partial class GoogleSheetsCategoryRepository
{
    [LoggerMessage(
        EventId = AddingCategory,
        Level = LogLevel.Debug,
        Message = "Preparing to add category '{Id}' to Google Sheets")]
    partial void LogAddingCategory(string id);

    [LoggerMessage(
        EventId = UpdatingCategory,
        Level = LogLevel.Debug,
        Message = "Preparing to update category '{Id}' in Google Sheets")]
    partial void LogUpdatingCategory(string id);

    [LoggerMessage(
        EventId = GettingAllCategories,
        Level = LogLevel.Debug,
        Message = "Preparing to get all categories from Google Sheets")]
    partial void LogGettingAllCategories();

    [LoggerMessage(
        EventId = CategoryAlreadyExists,
        Level = LogLevel.Information,
        Message = "Category '{Id}' already exists in Google Sheets")]
    partial void LogCategoryAlreadyExists(string id);
}
