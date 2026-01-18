using JournalApp.Data;
using JournalApp.Models.Sqlite;

namespace JournalApp.Services;

public class MoodService
{
    public async Task<List<Mood>> GetAllAsync()
    {
        var db = await JournalDatabase.GetConnectionAsync();
        return await db.Table<Mood>()
            .OrderBy(m => m.Category)
            .ThenBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<List<Mood>> GetByCategoryAsync(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return new List<Mood>();

        var db = await JournalDatabase.GetConnectionAsync();
        var trimmed = category.Trim();

        return await db.Table<Mood>()
            .Where(m => m.Category == trimmed)
            .OrderBy(m => m.Name)
            .ToListAsync();
    }
}
