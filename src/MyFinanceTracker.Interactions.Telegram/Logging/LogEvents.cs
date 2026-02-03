namespace MyFinanceTracker.Interactions.Telegram.Logging;

using static MyFinanceTracker.Common.Logging.LogEventRanges;

internal static class LogEvents
{
    private const int Base = InteractionsTelegram;

    public static class Worker
    {
        private const int SubBase = Base + 0;

        public const int Started = SubBase + 0;
        public const int PollingError = SubBase + 1;
        public const int UpdateReceived = SubBase + 2;
        public const int UnauthorizedAccess = SubBase + 3;
        public const int MessageSendFailed = SubBase + 4;
    }
}