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

        var mostFrequentMood = moodCounts.Count > 0
            ? moodCounts[0].Name
            : "-";

        var tagRows = await db.QueryAsync<TagCountRow>(
            @"SELECT Tag.Name AS Name, COUNT(*) AS Count
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

        var wordRows = await db.QueryAsync<EntryContentRow>(
            @"SELECT EntryDate AS EntryDate, Content AS Content
              FROM JournalEntry
              WHERE EntryDate >= ? AND EntryDate <= ?
              ORDER BY EntryDate ASC",
            start, end);

        var wordTrend = wordRows
            .GroupBy(r => JournalDatabase.NormalizeEntryDate(r.EntryDate))
            .Select(g => new WordCountPoint(g.Key, g.Sum(x => CountWords(x.Content))))
            .OrderBy(p => p.Date)
            .ToList();

        return new AnalyticsSummary(
            start,
            end,
            categoryCounts,
            moodCounts,
            mostFrequentMood,
            tagCounts,
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
        List<MoodCountItem> MoodCounts,
        string MostFrequentMood,
        List<TagCountItem> TagCounts,
        List<WordCountPoint> WordCountTrend);

    public record CategoryCountItem(string Category, int Count);
    public record MoodCountItem(string Name, string Category, int Count);
    public record TagCountItem(string Name, int Count);
    public record WordCountPoint(DateTime Date, int Count);

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

    private sealed class EntryContentRow
    {
        public DateTime EntryDate { get; set; }
        public string? Content { get; set; }
    }
}
