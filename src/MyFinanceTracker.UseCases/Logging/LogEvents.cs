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
            
            public const int CategoryNotFound = SubBase + 4;
            public const int CategoryRequired = SubBase + 5;
            
            public const int SystemError = SubBase + 100;
        }

        public static class Delete
        {
            private const int SubBase = Base + 1_000;
            
            public const int Starting = SubBase + 1;
            public const int Completed = SubBase + 2;
            public const int ValidationFailed = SubBase + 3;
            
            public const int CategoryNotFound = SubBase + 4;
            
            public const int SystemError = SubBase + 100;
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
            public const int SystemError = SubBase + 100;
        }
    }
}