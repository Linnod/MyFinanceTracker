using MyFinanceTracker.Infrastructure.Persistence.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Models;

internal sealed record AddCategoryOutboxPayload(CategorySyncRecord Category);
