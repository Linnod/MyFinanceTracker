using LogRanges = MyFinanceTracker.Common.Logging.LogEventRanges.Persistence;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Logging;

internal static class LogEvents
{
    private const int Base = LogRanges.GoogleSheets;

    public static class Client
    {
        private const int SubBase = Base + 0;

        public const int FetchingFormulas = SubBase + 1;
        public const int FormulasFetched = SubBase + 2;
        public const int SendingBatchUpdate = SubBase + 3;
        public const int BatchUpdateApplied = SubBase + 4;
    }

    public static class Repository
    {
        private const int SubBase = Base + 1_000;

        public const int AddingTransactions = SubBase + 1;
        public const int DeletingTransactions = SubBase + 2;
        public const int GettingTransactions = SubBase + 3;
    }
}