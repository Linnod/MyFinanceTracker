using LogRanges = MyFinanceTracker.Common.Logging.LogEventRanges.CommandProcessing.Text;

namespace MyFinanceTracker.CommandProcessing.Text.Regex.Logging;

internal static class LogEvents
{
    private const int Base = LogRanges.Regex;

    public static class Processor
    {
        private const int SubBase = Base + 1_000;

        public const int Entry = SubBase + 1;
        public const int Exit = SubBase + 2;
    }

    public static class Interpreter
    {
        private const int SubBase = Base + 2_000;

        public const int Entry = SubBase + 1;
        public const int Success = SubBase + 2;
        public const int EmptyInput = SubBase + 3;
        public const int UnrecognizedCommand = SubBase + 4;
    }

    public static class AddCommandHandler
    {
        private const int SubBase = Base + 3_000;

        public const int Entry = SubBase + 1;
        public const int ParseSuccess = SubBase + 2;
        public const int ParseFailure = SubBase + 3;
        public const int Exit = SubBase + 4;
    }

    public static class DeleteCommandHandler
    {
        private const int SubBase = Base + 4_000;

        public const int Entry = SubBase + 1;
        public const int ParseSuccess = SubBase + 2;
        public const int ParseFailure = SubBase + 3;
        public const int Exit = SubBase + 4;
    }

    public static class ListCategoriesCommandHandler
    {
        private const int SubBase = Base + 5_000;

        public const int Entry = SubBase + 1;
        public const int Exit = SubBase + 4;
    }
}