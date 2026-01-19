using JournalApp.Data;
using JournalApp.Models.Sqlite;

namespace JournalApp.Services;

public class TagService
{
    public async Task<List<Tag>> GetAllAsync()
    {
        var db = await JournalDatabase.GetConnectionAsync();
        return await db.Table<Tag>()
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<Tag> AddCustomAsync(string name)
    {
        return await JournalDatabase.AddTagAsync(name, isPrebuilt: false, category: "Custom");
    }

    public async Task SetTagsForEntryAsync(int entryId, List<int> tagIds)
    {
        var db = await JournalDatabase.GetConnectionAsync();
        var distinctTagIds = (tagIds ?? new List<int>()).Distinct().ToList();

        var existing = await db.Table<EntryTag>()
            .Where(et => et.EntryId == entryId)
            .ToListAsync();

        if (existing.Count > 0)
        {
            foreach (var tag in existing)
                await db.DeleteAsync(tag);
        }

        if (distinctTagIds.Count == 0) return;

        foreach (var tagId in distinctTagIds)
        {
            await db.InsertAsync(new EntryTag
            {
                EntryId = entryId,
                TagId = tagId
            });
        }
    }

    public async Task<List<Tag>> GetTagsForEntryAsync(int entryId)
    {
        var db = await JournalDatabase.GetConnectionAsync();

        var entryTags = await db.Table<EntryTag>()
            .Where(et => et.EntryId == entryId)
            .ToListAsync();

        if (entryTags.Count == 0) return new List<Tag>();

        var tagIds = entryTags.Select(et => et.TagId).ToList();

        return await db.Table<Tag>()
            .Where(t => tagIds.Contains(t.Id))
            .OrderBy(t => t.Name)
            .ToListAsync();
    }
}
