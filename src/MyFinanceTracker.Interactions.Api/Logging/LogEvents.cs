using LogRanges = MyFinanceTracker.Common.Logging.LogEventRanges.Interactions;

namespace MyFinanceTracker.Interactions.Api.Logging;

internal static class LogEvents
{
    private const int Base = LogRanges.Api;

    public static class Worker
    {
        private const int SubBase = Base + 0;

        public const int Started = SubBase + 1;
        public const int Stopped = SubBase + 2;
        public const int RequestReceived = SubBase + 3;
        public const int RequestFinished = SubBase + 4;
        public const int UnauthorizedAccess = SubBase + 5;
    }
}