using JournalApp.Models;

namespace JournalApp.Services;

public class JournalQueryService
{
    private readonly AppDataContext _context;

    public JournalQueryService(AppDataContext context)
    {
        _context = context;
    }

    public List<DiaryEntry> FindEntries(
        Guid userId,
        string? searchText,
        DateOnly? start,
        DateOnly? end,
        Mood? mood,
        List<Guid>? requiredTags)
    {
        IEnumerable<DiaryEntry> query =
            _context.Entries.Where(e => e.OwnerId == userId);

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(e =>
                (!string.IsNullOrWhiteSpace(e.Title) &&
                 e.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(e.Content) &&
                 e.Content.Contains(searchText, StringComparison.OrdinalIgnoreCase)));
        }

        if (start is not null)
            query = query.Where(e => e.EntryDay >= start.Value);

        if (end is not null)
            query = query.Where(e => e.EntryDay <= end.Value);

        
        if (mood is not null)
        {
            query = query.Where(e =>
                e.Moods is not null &&
                e.Moods.Contains(mood.Value));
        }

        if (requiredTags is { Count: > 0 })
        {
            query = query.Where(e =>
                requiredTags.All(id =>
                    e.Tags.Any(t => t.TagId == id)));
        }

        return query
            .OrderByDescending(e => e.EntryDay)
            .ToList();
    }
}
