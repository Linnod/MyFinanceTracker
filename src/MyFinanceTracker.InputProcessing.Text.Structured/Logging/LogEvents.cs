using LogRanges = MyFinanceTracker.Common.Logging.LogEventRanges.InputProcessing.Text;

namespace MyFinanceTracker.InputProcessing.Text.Structured.Logging;

internal static class LogEvents
{
    private const int Base = LogRanges.Structured;

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
        public const int Unrecognized = SubBase + 3;
    }

    public static class Commands
    {
        public static class Add
        {
            private const int SubBase = Base + 3_000;

            public const int Entry = SubBase + 1;
            public const int Exit = SubBase + 2;

            public static class Parser
            {
                private const int ParserSubBase = SubBase + 500;

                public const int ParseSuccess = ParserSubBase + 1;
                public const int ParseFailure = ParserSubBase + 2;
            }
        }

        public static class Delete
        {
            private const int SubBase = Base + 4_000;

            public const int Entry = SubBase + 1;
            public const int Exit = SubBase + 2;

            public static class Parser
            {
                private const int ParserSubBase = SubBase + 500;

                public const int ParseSuccess = ParserSubBase + 1;
                public const int ParseFailure = ParserSubBase + 2;
            }
        }

        public static class ListCategories
        {
            private const int SubBase = Base + 5_000;

            public const int Entry = SubBase + 1;
            public const int Exit = SubBase + 2;
        }
    }
}