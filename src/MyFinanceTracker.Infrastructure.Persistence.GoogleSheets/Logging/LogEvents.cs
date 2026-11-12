using LogRanges = MyFinanceTracker.Common.Logging.LogEventRanges.Persistence;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Logging;

internal static class LogEvents
{
    private const int Base = LogRanges.GoogleSheets;

    public static class Repository
    {
        private const int SubBase = Base + 0;

        public const int AddingTransactions = SubBase + 1;
        public const int DeletingTransactions = SubBase + 2;
        public const int GettingTransactions = SubBase + 3;
        public const int GettingTransactionById = SubBase + 4;
        public const int UpdatingTransaction = SubBase + 5;
        public const int GettingAllTransactions = SubBase + 6;
    }

    public static class CategoryRepository
    {
        private const int SubBase = Base + 100;

        public const int AddingCategory = SubBase + 1;
        public const int GettingCategoryById = SubBase + 2;
        public const int GettingCategoriesByIds = SubBase + 3;
        public const int UpdatingCategory = SubBase + 4;
        public const int GettingAllCategories = SubBase + 5;
    }

    public static class Client
    {
        private const int SubBase = Base + 1_000;

        public const int FetchingRows = SubBase + 1;
        public const int RowsFetched = SubBase + 2;
        public const int AppendingRows = SubBase + 3;
        public const int RowsAppended = SubBase + 4;
        public const int DeletingRows = SubBase + 5;
        public const int RowsDeleted = SubBase + 6;
        public const int UpdatingRow = SubBase + 7;
        public const int RowUpdated = SubBase + 8;
    }
}
