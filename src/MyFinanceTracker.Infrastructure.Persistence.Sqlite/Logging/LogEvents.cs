using LogRanges = MyFinanceTracker.Common.Logging.LogEventRanges.Persistence;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Logging;

internal static class LogEvents
{
    private const int Base = LogRanges.Sqlite;

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
        private const int SubBase = Base + 3_000;

        public const int AddingCategory = SubBase + 1;
        public const int GettingCategoryById = SubBase + 2;
        public const int GettingCategoriesByIds = SubBase + 3;
        public const int UpdatingCategory = SubBase + 4;
        public const int GettingAllCategories = SubBase + 5;
    }

    public static class Sync
    {
        private const int SubBase = Base + 1_000;

        public const int StartingSync = SubBase + 1;
        public const int SyncCompleted = SubBase + 2;
        public const int SyncFailed = SubBase + 3;
        public const int ProcessingOutboxMessages = SubBase + 4;
        public const int OutboxMessageProcessed = SubBase + 5;
        public const int SyncStopped = SubBase + 6;
        public const int RetryScheduled = SubBase + 7;
        public const int MovedToDeadLetter = SubBase + 8;
        public const int NotificationPublishFailed = SubBase + 9;
    }

    public static class Initializer
    {
        private const int SubBase = Base + 2_000;

        public const int InitializingDatabase = SubBase + 1;
        public const int DatabaseInitialized = SubBase + 2;
        public const int FinalOutboxProcessingFailed = SubBase + 3;
        public const int FlushingOutbox = SubBase + 4;
        public const int FetchingTransactions = SubBase + 5;
        public const int SyncingTransactions = SubBase + 6;
        public const int StoppingInitialization = SubBase + 7;
        public const int StopCompleted = SubBase + 8;
        public const int FetchingCategories = SubBase + 9;
        public const int SyncingCategories = SubBase + 10;
    }
}
