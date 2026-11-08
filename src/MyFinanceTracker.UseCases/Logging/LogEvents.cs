using CoreRanges = MyFinanceTracker.Common.Logging.LogEventRanges.UseCases;

namespace MyFinanceTracker.UseCases.Logging;

public static class LogEvents
{
    public static class Transactions
    {
        private const int Base = CoreRanges.Transactions;

        public static class Create
        {
            private const int SubBase = Base + 0;

            public const int Starting = SubBase + 1;
            public const int Completed = SubBase + 2;
            public const int ValidationFailed = SubBase + 3;
            public const int Failure = SubBase + 4;
            public const int CriticalError = SubBase + 5;
        }

        public static class Delete
        {
            private const int SubBase = Base + 1_000;

            public const int Starting = SubBase + 1;
            public const int Completed = SubBase + 2;
            public const int ValidationFailed = SubBase + 3;
            public const int Failure = SubBase + 4;
            public const int CriticalError = SubBase + 5;
        }

        public static class Get
        {
            private const int SubBase = Base + 2_000;

            public const int Starting = SubBase + 1;
            public const int Completed = SubBase + 2;
            public const int ValidationFailed = SubBase + 3;
            public const int Failure = SubBase + 4;
            public const int CriticalError = SubBase + 5;
        }

        public static class Update
        {
            private const int SubBase = Base + 3_000;

            public const int Starting = SubBase + 1;
            public const int Completed = SubBase + 2;
            public const int ValidationFailed = SubBase + 3;
            public const int Failure = SubBase + 4;
            public const int CriticalError = SubBase + 5;
        }
    }

    public static class Categories
    {
        private const int Base = CoreRanges.Categories;

        public static class List
        {
            private const int SubBase = Base + 0;

            public const int Starting = SubBase + 1;
            public const int Completed = SubBase + 2;
            public const int CriticalError = SubBase + 3;
        }
    }
}
