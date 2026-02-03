namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Logging;

using static MyFinanceTracker.Common.Logging.LogEventRanges;

internal static class LogEvents
{
    public static class GoogleSheets
    {
        public static class Client
        {
            private const int SubBase = Infrastructure + 100;

            public const int FetchingFormulas = SubBase + 1;
            public const int FormulasFetched = SubBase + 2;
            public const int SendingBatchUpdate = SubBase + 3;
            public const int BatchUpdateApplied = SubBase + 4;

        }

        public static class Repository
        {
            private const int SubBase = Infrastructure + 200;

            public const int AddingTransactions = SubBase + 1;
            public const int DeletingTransactions = SubBase + 2;
        }
    }
}
