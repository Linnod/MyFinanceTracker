namespace MyFinanceTracker.Common.Utilities;

public static class FuzzyMatcher
{
    public static string? GetClosest(string input, IEnumerable<string> candidates, int maxDistance = 2)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        if (input.Length < maxDistance)
        {
            return null;
        }

        var result = candidates
            .Select(c => new { Value = c, Distance = ComputeDistance(input, c) })
            .Where(x => x.Distance <= maxDistance)
            .OrderBy(x => x.Distance)
            .ThenBy(x => x.Value.Length)
            .FirstOrDefault();

        return result?.Value;
    }

    private static int ComputeDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s))
        {
             return t?.Length ?? 0;
        }

        if (string.IsNullOrEmpty(t))
        {
             return s?.Length ?? 0;
        }

        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; i++)
        {
            d[i, 0] = i;
        }

        for (int j = 0; j <= m; j++)
        {
            d[0, j] = j;
        }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);

                if (i > 1 && j > 1 && s[i - 1] == t[j - 2] && s[i - 2] == t[j - 1])
                {
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost);
                }
            }
        }
        return d[n, m];
    }
}