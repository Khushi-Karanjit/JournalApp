using System.Text.RegularExpressions;
using CommunityToolkit.Maui.Storage;
using JournalApp.Data;
using JournalApp.Models.Sqlite;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace JournalApp.Services;

public class PdfExportService
{
    private readonly EntryService _entryService;

    public PdfExportService(EntryService entryService)
    {
        _entryService = entryService;
    }

    public async Task<ExportResult> ExportEntriesAsync(DateTime startDate, DateTime endDate)
    {
        var entries = await _entryService.GetEntriesInRangeAsync(startDate, endDate);
        var db = await JournalDatabase.GetConnectionAsync();

        var moods = await db.Table<Mood>().ToListAsync();
        var moodById = moods.ToDictionary(m => m.Id, m => m.Name);

        var tags = await db.Table<Tag>().ToListAsync();
        var tagById = tags.ToDictionary(t => t.Id, t => t.Name);

        var entryTags = await db.Table<EntryTag>().ToListAsync();
        var tagsByEntry = entryTags
            .GroupBy(t => t.EntryId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.TagId).ToList());

        var fileName = $"Journal_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf";

        QuestPDF.Settings.License = LicenseType.Community;
        var pdfBytes = BuildDocument(entries, moodById, tagById, tagsByEntry);

        using var stream = new MemoryStream(pdfBytes);
        var result = await FileSaver.Default.SaveAsync(fileName, stream);

        return result.IsSuccessful
            ? ExportResult.Success(result.FilePath ?? fileName)
            : ExportResult.Failure(result.Exception?.Message ?? "Export failed.");
    }

    private static byte[] BuildDocument(
        List<JournalEntry> entries,
        Dictionary<int, string> moodById,
        Dictionary<int, string> tagById,
        Dictionary<int, List<int>> tagsByEntry)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(24);
                page.DefaultTextStyle(TextStyle.Default.FontSize(11));
                page.Content().Column(col =>
                {
                    col.Item().Text("Journal Export").FontSize(18).SemiBold();
                    col.Item().Height(10);

                    foreach (var entry in entries.OrderByDescending(e => e.EntryDate))
                    {
                        var primary = moodById.TryGetValue(entry.PrimaryMoodId, out var p) ? p : "-";
                        var secondary = new List<string>();
                        if (entry.SecondaryMood1Id.HasValue && moodById.TryGetValue(entry.SecondaryMood1Id.Value, out var s1))
                            secondary.Add(s1);
                        if (entry.SecondaryMood2Id.HasValue && moodById.TryGetValue(entry.SecondaryMood2Id.Value, out var s2))
                            secondary.Add(s2);

                        var tagNames = tagsByEntry.TryGetValue(entry.Id, out var tagIds)
                            ? tagIds.Where(id => tagById.ContainsKey(id)).Select(id => tagById[id]).ToList()
                            : new List<string>();

                        col.Item().BorderBottom(1).PaddingBottom(6).Column(item =>
                        {
                            item.Item().Text($"{entry.EntryDate:yyyy-MM-dd} - {entry.Title}").SemiBold();
                            item.Item().Text($"Mood: {primary}{(secondary.Count > 0 ? " (+" + string.Join(", ", secondary) + ")" : "")}");
                            item.Item().Text($"Tags: {(tagNames.Count > 0 ? string.Join(", ", tagNames) : "-")}");
                            item.Item().Text("Content:");
                            item.Item().Text(ToPlainText(entry.Content ?? ""));
                        });

                        col.Item().Height(8);
                    }
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string ToPlainText(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";
        return Regex.Replace(html, "<.*?>", " ").Trim();
    }

    public record ExportResult(bool Ok, string Message)
    {
        public static ExportResult Success(string path) =>
            new(true, $"Exported to: {path}");

        public static ExportResult Failure(string error) =>
            new(false, error);
    }
}
