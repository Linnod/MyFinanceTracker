namespace MyFinanceTracker.UseCases.Logging;

using static MyFinanceTracker.Common.Logging.LogEventRanges;

public static class LogEvents
{
    public static class Transactions
    {
        public static class Create
        {
            private const int SubBase = UseCases + 100;
            
            public const int Starting = SubBase + 1;
            public const int Completed = SubBase + 2;
            public const int ValidationFailed = SubBase + 3;
            public const int SystemError = SubBase + 4;
        }

        public static class Delete
        {
            private const int SubBase = UseCases + 200;
            
            public const int Starting = SubBase + 1;
            public const int Completed = SubBase + 2;
            public const int ValidationFailed = SubBase + 3;
            public const int SystemError = SubBase + 4;
        }
    }

    public static class Categories
    {
        public static class List
        {
            private const int SubBase = UseCases + 300;

            public const int Starting = SubBase + 1;
            public const int Completed = SubBase + 2;
            //public const int ValidationFailed = SubBase + 3;
            public const int SystemError = SubBase + 4;

        }
    }
}
