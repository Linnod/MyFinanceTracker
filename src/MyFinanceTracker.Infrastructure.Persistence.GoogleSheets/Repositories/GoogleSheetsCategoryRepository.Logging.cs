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
        EventId = GettingCategoryById,
        Level = LogLevel.Debug,
        Message = "Preparing to get category with ID '{Id}' from Google Sheets")]
    partial void LogGettingCategoryById(string id);

    [LoggerMessage(
        EventId = GettingCategoriesByIds,
        Level = LogLevel.Debug,
        Message = "Preparing to get {Count} categories by IDs from Google Sheets")]
    partial void LogGettingCategoriesByIds(int count);

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

    [LoggerMessage(
        EventId = DuplicateCategoriesDetected,
        Level = LogLevel.Warning,
        Message = "Duplicate categories detected in Google Sheets: {DuplicateIds}")]
    partial void LogDuplicateCategoriesDetected(string duplicateIds);
}
