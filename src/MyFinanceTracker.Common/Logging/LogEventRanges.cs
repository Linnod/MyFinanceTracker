namespace MyFinanceTracker.Common.Logging;

public static class LogEventRanges
{
    public static class Interactions
    {
        public const int Console = 10_100_000;
        public const int Telegram = 10_200_000;
        public const int Api = 10_300_000;
    }

    public static class CommandProcessing
    {
        public const int Text = 20_100_000;
    }

    public static class UseCases
    {
        public const int Transactions = 30_100_000;
        public const int Categories = 30_200_000;
    }

    public static class Persistence
    {
        public const int Yaml = 40_100_000;
        public const int GoogleSheets = 40_200_000; 
    }
}