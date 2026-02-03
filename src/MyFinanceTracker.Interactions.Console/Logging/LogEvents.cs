namespace MyFinanceTracker.Interactions.Console.Logging;

using static MyFinanceTracker.Common.Logging.LogEventRanges;

internal static class LogEvents
{
    private const int Base = InteractionsConsole;

    public static class Worker
    {
        private const int SubBase = Base + 0;

        public const int Started = SubBase + 0;
        public const int CommandReceived = SubBase + 2;
        public const int ProcessingError = SubBase + 3;
        public const int FatalError = SubBase + 4;
    }
}