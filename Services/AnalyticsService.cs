using System.Text.RegularExpressions;
using JournalApp.Data;
using JournalApp.Models.Sqlite;

namespace JournalApp.Services;

public class AnalyticsService
{
    public async Task<AnalyticsSummary> GetSummaryAsync(DateTime startDate, DateTime endDate)
    {
        var start = JournalDatabase.NormalizeEntryDate(startDate);
        var end = JournalDatabase.NormalizeEntryDate(endDate);
        if (end < start)
        {
            var temp = start;
            start = end;
            end = temp;
        }

        var db = await JournalDatabase.GetConnectionAsync();

        var totalEntries = await db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM JournalEntry WHERE EntryDate >= ? AND EntryDate <= ?",
            start, end);

        var moodRows = await db.QueryAsync<MoodCountRow>(
            @"SELECT Mood.Category AS Category, Mood.Name AS Name, COUNT(*) AS Count
              FROM (
                SELECT PrimaryMoodId AS MoodId, EntryDate FROM JournalEntry
                UNION ALL
                SELECT SecondaryMood1Id AS MoodId, EntryDate FROM JournalEntry WHERE SecondaryMood1Id IS NOT NULL
                UNION ALL
                SELECT SecondaryMood2Id AS MoodId, EntryDate FROM JournalEntry WHERE SecondaryMood2Id IS NOT NULL
              ) AS MoodPick
              JOIN Mood ON Mood.Id = MoodPick.MoodId
              WHERE MoodPick.EntryDate >= ? AND MoodPick.EntryDate <= ?
              GROUP BY Mood.Category, Mood.Name
              ORDER BY Count DESC",
            start, end);

        var moodCounts = moodRows
            .Select(r => new MoodCountItem(r.Name, r.Category, r.Count))
            .ToList();

        var categoryCounts = moodCounts
            .GroupBy(m => m.Category)
            .Select(g => new CategoryCountItem(g.Key, g.Sum(x => x.Count)))
            .OrderByDescending(c => c.Count)
            .ToList();

        var categoryPercentages = categoryCounts
            .Select(c => new CategoryPercentItem(
                c.Category,
                c.Count,
                totalEntries == 0 ? 0 : Math.Round((double)c.Count / totalEntries * 100, 1)))
            .ToList();

        var mostFrequentMood = moodCounts.Count > 0
            ? moodCounts[0].Name
            : "-";

        var tagRows = await db.QueryAsync<TagCountRow>(
            @"SELECT Tag.Name AS Name, COUNT(DISTINCT EntryTag.EntryId) AS Count
              FROM EntryTag
              JOIN Tag ON Tag.Id = EntryTag.TagId
              JOIN JournalEntry ON JournalEntry.Id = EntryTag.EntryId
              WHERE JournalEntry.EntryDate >= ? AND JournalEntry.EntryDate <= ?
              GROUP BY Tag.Name
              ORDER BY Count DESC",
            start, end);

        var tagCounts = tagRows
            .Select(r => new TagCountItem(r.Name, r.Count))
            .ToList();

        var tagCategoryRows = await db.QueryAsync<TagCategoryRow>(
            @"SELECT Tag.Category AS Category, COUNT(DISTINCT EntryTag.EntryId) AS Count
              FROM EntryTag
              JOIN Tag ON Tag.Id = EntryTag.TagId
              JOIN JournalEntry ON JournalEntry.Id = EntryTag.EntryId
              WHERE JournalEntry.EntryDate >= ? AND JournalEntry.EntryDate <= ?
              GROUP BY Tag.Category
              ORDER BY Count DESC",
            start, end);

        var tagBreakdown = tagCategoryRows
            .Select(t => new TagPercentItem(
                t.Category,
                t.Count,
                totalEntries == 0 ? 0 : Math.Round((double)t.Count / totalEntries * 100, 1)))
            .ToList();

        var wordRows = await db.QueryAsync<EntryContentRow>(
            @"SELECT EntryDate AS EntryDate, Content AS Content
              FROM JournalEntry
              WHERE EntryDate >= ? AND EntryDate <= ?
              ORDER BY EntryDate ASC",
            start, end);

        var wordTrend = wordRows
            .GroupBy(r => JournalDatabase.NormalizeEntryDate(r.EntryDate))
            .Select(g =>
            {
                var total = g.Sum(x => CountWords(x.Content));
                var avg = g.Count() == 0 ? 0 : (double)total / g.Count();
                return new WordCountPoint(g.Key, Math.Round(avg, 1));
            })
            .OrderBy(p => p.Date)
            .ToList();

        var entryDates = await db.QueryAsync<EntryDateRow>(
            "SELECT EntryDate FROM JournalEntry WHERE EntryDate >= ? AND EntryDate <= ?",
            start, end);
        var dateSet = entryDates
            .Select(d => JournalDatabase.NormalizeEntryDate(d.EntryDate))
            .ToHashSet();

        var streak = CalculateCurrentStreak(dateSet, end);
        var longestStreak = CalculateLongestStreak(dateSet);
        var missedDates = CalculateMissedDates(dateSet, start, end);

        return new AnalyticsSummary(
            start,
            end,
            categoryCounts,
            categoryPercentages,
            moodCounts,
            mostFrequentMood,
            tagCounts,
            tagBreakdown,
            streak,
            longestStreak,
            missedDates,
            wordTrend);
    }

    private static int CountWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        var noHtml = Regex.Replace(text, "<.*?>", " ");
        return noHtml
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Length;
    }

    public record AnalyticsSummary(
        DateTime StartDate,
        DateTime EndDate,
        List<CategoryCountItem> MoodCategoryCounts,
        List<CategoryPercentItem> MoodCategoryPercentages,
        List<MoodCountItem> MoodCounts,
        string MostFrequentMood,
        List<TagCountItem> TagCounts,
        List<TagPercentItem> TagBreakdownPercentages,
        int CurrentStreakDays,
        int LongestStreakDays,
        List<DateTime> MissedDates,
        List<WordCountPoint> WordCountTrend);

    public record CategoryCountItem(string Category, int Count);
    public record CategoryPercentItem(string Category, int Count, double Percentage);
    public record MoodCountItem(string Name, string Category, int Count);
    public record TagCountItem(string Name, int Count);
    public record TagPercentItem(string Category, int Count, double Percentage);
    public record WordCountPoint(DateTime Date, double AverageWords);

    private sealed class MoodCountRow
    {
        public string Category { get; set; } = "";
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    private sealed class TagCountRow
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    private sealed class TagCategoryRow
    {
        public string Category { get; set; } = "";
        public int Count { get; set; }
    }

    private sealed class EntryContentRow
    {
        public DateTime EntryDate { get; set; }
        public string? Content { get; set; }
    }

    private sealed class EntryDateRow
    {
        public DateTime EntryDate { get; set; }
    }

    private static int CalculateCurrentStreak(HashSet<DateTime> dates, DateTime endDate)
    {
        var cursor = endDate.Date;
        if (!dates.Contains(cursor)) return 0;

        var streak = 0;
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
}
