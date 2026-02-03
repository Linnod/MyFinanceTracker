using MyFinanceTracker.Common.Logging;

namespace MyFinanceTracker.CommandProcessing.Text.Logging;

using static LogEventRanges;

internal static class LogEvents
{
    private const int Base = CommandProcessing;

    public static class Receiver
    {
        private const int SubBase = Base + 0;
        public const int Entry = SubBase + 0;
        public const int Exit = SubBase + 1;
        public const int CriticalError = SubBase + 2;
    }

    public static class Processor
    {
        private const int SubBase = Base + 10;
        public const int Entry = SubBase + 0;
        public const int Exit = SubBase + 1;
    }

    public static class Interpreter
    {
        private const int SubBase = Base + 20;
        public const int Entry = SubBase + 0;
        public const int Success = SubBase + 1;
        public const int EmptyInput = SubBase + 2;
        public const int UnrecognizedCommand = SubBase + 3;
    }

    public static class Dispatcher
    {
        private const int SubBase = Base + 30;
        public const int Entry = SubBase + 0;
        public const int HandlerFound = SubBase + 1;
        public const int HandlerNotFound = SubBase + 2;
    }

    public static class AddCommandHandler
    {
        private const int SubBase = Base + 100;
        public const int Entry = SubBase + 0;
        public const int ParseSuccess = SubBase + 1;
        public const int ParseFailure = SubBase + 2;
        public const int SystemError = SubBase + 3;
        public const int Exit = SubBase + 4;
    }

    public static class DeleteCommandHandler
    {
        private const int SubBase = Base + 110;
        public const int Entry = SubBase + 0;
        public const int ParseSuccess = SubBase + 1;
        public const int ParseFailure = SubBase + 2;
        public const int SystemError = SubBase + 3;
        public const int Exit = SubBase + 4;
    }
}
