// We use Regex to clean up and parse the HTML content from our journal editor
using System.Text.RegularExpressions;
// This toolkit helps us save files to the user's device easily
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

    // This is the main method that handles exporting multiple entries as a PDF
    public async Task<ExportResult> ExportEntriesAsync(DateTime startDate, DateTime endDate)
    {
        // First, we grab all the entries between the dates the user selected
        var entries = await _entryService.GetEntriesInRangeAsync(startDate, endDate);
        var db = await JournalDatabase.GetConnectionAsync();

        // We need to map Mood and Tag IDs to their actual names so the PDF shows "Happy" instead of "1"
        var moods = await db.Table<Mood>().ToListAsync();
        var moodById = moods.ToDictionary(m => m.Id, m => m.Name);
        var categoryByMoodId = moods.ToDictionary(m => m.Id, m => m.Category);

        var tags = await db.Table<Tag>().ToListAsync();
        var tagById = tags.ToDictionary(t => t.Id, t => t.Name);

        var entryTags = await db.Table<EntryTag>().ToListAsync();
        var tagsByEntry = entryTags
            .GroupBy(t => t.EntryId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.TagId).ToList());

        var fileName = $"Journal_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf";

        QuestPDF.Settings.License = LicenseType.Community;
        var pdfBytes = BuildDocument(entries, moodById, categoryByMoodId, tagById, tagsByEntry);

        using var stream = new MemoryStream(pdfBytes);
        var result = await FileSaver.Default.SaveAsync(fileName, stream);

        return result.IsSuccessful
            ? ExportResult.Success(result.FilePath ?? fileName)
            : ExportResult.Failure(result.Exception?.Message ?? "Export failed.");
    }

    // This private method builds the actual layout of the PDF document using QuestPDF
    private static byte[] BuildDocument(
        List<JournalEntry> entries,
        Dictionary<int, string> moodById,
        Dictionary<int, string> categoryByMoodId,
        Dictionary<int, string> tagById,
        Dictionary<int, List<int>> tagsByEntry)
    {
        // We create a document container where we define margins, fonts, and dynamic content
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
                        var category = !string.IsNullOrWhiteSpace(entry.Category)
                            ? entry.Category
                            : (categoryByMoodId.TryGetValue(entry.PrimaryMoodId, out var c) ? c : "-");
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
                            item.Item().Text($"Category: {category}");
                            item.Item().Text($"Tags: {(tagNames.Count > 0 ? string.Join(", ", tagNames) : "-")}");
                            item.Item().PaddingTop(4).Text("Content:").SemiBold();
                            
                            RenderHtmlContent(item, entry.Content ?? "");
                        });

                        col.Item().Height(8);
                    }
                });
            });
        });

        return document.GeneratePdf();
    }

    // Since QuestPDF doesn't support HTML directly, we built this simple parser to handle basic tags
    private static void RenderHtmlContent(ColumnDescriptor col, string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return;

        // We split the HTML string into "blocks" like paragraphs (<p>) and list items (<li>)
        var blocks = Regex.Split(html, @"(?=<(?:p|li|ul|ol|h[1-6]|blockquote)[^>]*>)");

        foreach (var block in blocks)
        {
            if (string.IsNullOrWhiteSpace(block)) continue;

            var cleanBlock = block.Trim();
            // Check if this block is a list item so we can add a bullet point symbol
            bool isListItem = cleanBlock.StartsWith("<li", StringComparison.OrdinalIgnoreCase);
            bool isHeading = Regex.IsMatch(cleanBlock, @"^<h[1-6]", RegexOptions.IgnoreCase);

            col.Item().Row(row =>
            {
                if (isListItem)
                {
                    row.ConstantItem(15).Text("•");
                }

                row.RelativeItem().Text(text =>
                {
                    // Now we parse inline styles like bold and italic for the text inside the block
                    ParseInlineStyles(text, cleanBlock, isHeading);
                });
            });
        }
    }

    private static void ParseInlineStyles(TextDescriptor text, string html, bool isHeading)
    {
        // Remove block tags but keep inline ones
        var inlineContent = Regex.Replace(html, @"^<(?:p|li|ul|ol|h[1-6]|blockquote)[^>]*>|<\/(?:p|li|ul|ol|h[1-6]|blockquote)>", "");
        
        // Regex to find tags and text segments
        var matches = Regex.Matches(inlineContent, @"(<[^>]+>|[^<]+)");

        bool isBold = isHeading;
        bool isItalic = false;
        bool isUnderline = false;

        foreach (Match match in matches)
        {
            var val = match.Value;
            if (val.StartsWith("<"))
            {
                var tag = val.ToLower();
                if (tag == "<strong>" || tag == "<b>") isBold = true;
                else if (tag == "</strong>" || tag == "</b>") isBold = isHeading;
                else if (tag == "<em>" || tag == "<i>") isItalic = true;
                else if (tag == "</em>" || tag == "</i>") isItalic = false;
                else if (tag == "<u>") isUnderline = true;
                else if (tag == "</u>") isUnderline = false;
                else if (tag == "<br>" || tag == "<br/>") text.EmptyLine();
            }
            else
            {
                var content = System.Net.WebUtility.HtmlDecode(val);
                var span = text.Span(content);
                if (isBold) span.SemiBold();
                if (isItalic) span.Italic();
                if (isUnderline) span.Underline();
                if (isHeading) span.FontSize(13);
            }
        }
    }

    public record ExportResult(bool Ok, string Message)
    {
        public static ExportResult Success(string path) =>
            new(true, $"Exported to: {path}");

        public static ExportResult Failure(string error) =>
            new(false, error);
    }
}
