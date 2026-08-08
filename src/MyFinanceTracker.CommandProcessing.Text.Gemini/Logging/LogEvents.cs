using LogRanges = MyFinanceTracker.Common.Logging.LogEventRanges.CommandProcessing.Text;

namespace MyFinanceTracker.CommandProcessing.Text.Gemini.Logging;

internal static class LogEvents
{
    private const int Base = LogRanges.Gemini;

    public static class Processor
    {
        private const int SubBase = Base + 1_000;

        public const int Entry = SubBase + 1;
        public const int TextResponse = SubBase + 3;
        public const int Exit = SubBase + 4;
        public const int Error = SubBase + 5;
    }

    public static class Executor
    {
        private const int SubBase = Base + 2_000;

        public const int ExecutingTool = SubBase + 1;
        public const int ExecutedTool = SubBase + 2;
    }
}