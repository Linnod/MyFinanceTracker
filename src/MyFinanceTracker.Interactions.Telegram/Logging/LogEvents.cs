using LogRanges = MyFinanceTracker.Common.Logging.LogEventRanges.Interactions;

namespace MyFinanceTracker.Interactions.Telegram.Logging;

internal static class LogEvents
{
    private const int Base = LogRanges.Telegram;

    public static class Worker
    {
        private const int SubBase = Base + 0;

        public const int Started = SubBase + 1;
        public const int PollingError = SubBase + 2;
        public const int UpdateReceived = SubBase + 3;
        public const int UnauthorizedAccess = SubBase + 4;
        public const int MessageSendFailed = SubBase + 5;
        public const int InputBuildFailed = SubBase + 6;
        public const int IncomingMessageReceived = SubBase + 7;
    }

    public static class Notifications
    {
        private const int SubBase = Base + 1_000;

        public const int NotificationSendFailed = SubBase + 1;
    }
}
