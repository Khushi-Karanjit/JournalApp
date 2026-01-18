using JournalApp.Models;

namespace JournalApp.Services;

public class DiaryEntryService
{
    private readonly AppDataContext _context;

    public DiaryEntryService(AppDataContext context)
    {
        _context = context;
    }

    public DiaryEntry? GetEntryForDay(Guid userId, DateOnly day)
    {
        return _context.Entries
            .FirstOrDefault(e => e.OwnerId == userId && e.EntryDay == day);
    }

    // Feature 1: One entry per day (create or update)
    public DiaryEntry UpsertForDay(Guid userId, DateOnly day, DiaryEntry draft)
    {
        var existing = GetEntryForDay(userId, day);
        var now = DateTime.Now;

        if (existing is null)
        {
            var newEntry = new DiaryEntry
            {
                EntryId = Guid.NewGuid(),
                OwnerId = userId,
                EntryDay = day,

                Title = (draft.Title ?? "").Trim(),
                Content = draft.Content ?? "",

                // Single mood list
                Moods = draft.Moods?.ToList() ?? new List<Mood>(),

                Tags = draft.Tags?.ToList() ?? new List<Tag>(),

                CreatedOn = now,
                LastUpdatedOn = now
            };

            _context.Entries.Add(newEntry);
            return newEntry;
        }

        // Update existing
        existing.Title = (draft.Title ?? "").Trim();
        existing.Content = draft.Content ?? "";

        existing.Moods = draft.Moods?.ToList() ?? new List<Mood>();
        existing.Tags = draft.Tags?.ToList() ?? new List<Tag>();

        existing.LastUpdatedOn = now;

        return existing;
    }

    public bool DeleteForDay(Guid userId, DateOnly day)
    {
        var existing = GetEntryForDay(userId, day);
        if (existing is null) return false;

        _context.Entries.Remove(existing);
        return true;
    }
}
