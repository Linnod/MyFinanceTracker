using LogRanges = MyFinanceTracker.Common.Logging.LogEventRanges.CommandProcessing.Text;

namespace MyFinanceTracker.CommandProcessing.Text.Logging;

internal static class LogEvents
{
    private const int Base = LogRanges.Core;

    public static class Receiver
    {
        private const int SubBase = Base + 0;

        public const int Entry = SubBase + 1;
        public const int Exit = SubBase + 2;
        public const int CriticalError = SubBase + 3;
    }
}