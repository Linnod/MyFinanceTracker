using LogRanges = MyFinanceTracker.Common.Logging.LogEventRanges.Persistence;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml.Logging;

internal static class LogEvents
{
    private const int Base = LogRanges.Yaml;

    public static class Categories
    {
        private const int SubBase = Base + 0;

        public const int Loading = SubBase + 1;
        public const int Loaded = SubBase + 2;
        public const int LoadError = SubBase + 3;
    }
}