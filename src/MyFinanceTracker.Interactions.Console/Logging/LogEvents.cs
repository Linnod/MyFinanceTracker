using LogRanges = MyFinanceTracker.Common.Logging.LogEventRanges.Interactions;

namespace MyFinanceTracker.Interactions.Console.Logging;

internal static class LogEvents
{
    private const int Base = LogRanges.Console;

    public static class Worker
    {
        private const int SubBase = Base + 0;

        public const int Started = SubBase + 1;
        public const int CommandReceived = SubBase + 2;
        public const int ProcessingError = SubBase + 3;
        public const int FatalError = SubBase + 4;
    }
}