namespace JournalApp.Services;

public class StreakService
{
    private readonly EntryService _entryService = new();

    public async Task<StreakSummary> GetSummaryAsync(int rangeDays)
    {
        var today = DateTime.Today;

        var allDates = await _entryService.GetAllEntryDatesAsync();
        var currentStreak = CalculateCurrentStreak(allDates, today);
        var longestStreak = CalculateLongestStreak(allDates);

        var rangeStart = today.AddDays(-Math.Max(0, rangeDays - 1));
        var rangeDates = await _entryService.GetEntryDatesInRangeAsync(rangeStart, today);
        var missedDates = CalculateMissedDates(rangeDates, rangeStart, today);

        return new StreakSummary(currentStreak, longestStreak, missedDates);
    }

    private static int CalculateCurrentStreak(HashSet<DateTime> dates, DateTime today)
    {
        if (!dates.Contains(today)) return 0;

        var streak = 0;
        var cursor = today;
        while (dates.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }

    private static int CalculateLongestStreak(HashSet<DateTime> dates)
    {
        if (dates.Count == 0) return 0;

        var ordered = dates.OrderBy(d => d).ToList();
        var longest = 1;
        var current = 1;

        for (var i = 1; i < ordered.Count; i++)
        {
            var diff = (ordered[i] - ordered[i - 1]).Days;
            if (diff == 1)
            {
                current++;
                if (current > longest) longest = current;
            }
            else
            {
                current = 1;
            }
        }

        return longest;
    }

    private static List<DateTime> CalculateMissedDates(
        HashSet<DateTime> entryDates,
        DateTime start,
        DateTime end)
    {
        var missed = new List<DateTime>();
        var cursor = start.Date;
        var last = end.Date;

        while (cursor <= last)
        {
            if (!entryDates.Contains(cursor))
                missed.Add(cursor);

            cursor = cursor.AddDays(1);
        }

        return missed;
    }

    public record StreakSummary(int CurrentStreakDays, int LongestStreakDays, List<DateTime> MissedDates);
}
