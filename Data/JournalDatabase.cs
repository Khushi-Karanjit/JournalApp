using JournalApp.Models.Sqlite;
using Microsoft.Maui.Storage;
using SQLite;

namespace JournalApp.Data;

public static class JournalDatabase
{
    private static SQLiteAsyncConnection? _database;
    private static bool _initialized;
    private static readonly SemaphoreSlim InitLock = new(1, 1);

    public static string DbPath =>
        Path.Combine(FileSystem.AppDataDirectory, "journal.db3");

    public static async Task InitAsync()
    {
        if (_initialized) return;

        await InitLock.WaitAsync();
        try
        {
            if (_initialized) return;

            _database ??= new SQLiteAsyncConnection(DbPath);

            await _database.CreateTableAsync<JournalEntry>();
            await _database.CreateTableAsync<Mood>();
            await _database.CreateTableAsync<Tag>();
            await _database.CreateTableAsync<EntryTag>();

            _initialized = true;
        }
        finally
        {
            InitLock.Release();
        }
    }

    public static async Task<JournalEntry> UpsertEntryForDayAsync(JournalEntry entry)
    {
        if (_database is null) throw new InvalidOperationException("Database not initialized.");

        entry.EntryDate = NormalizeDate(entry.EntryDate);

        var existing = await _database.Table<JournalEntry>()
            .Where(e => e.EntryDate == entry.EntryDate)
            .FirstOrDefaultAsync();

        if (existing is null)
        {
            entry.CreatedAt = DateTime.UtcNow;
            entry.UpdatedAt = entry.CreatedAt;
            await _database.InsertAsync(entry);
            return entry;
        }

        entry.Id = existing.Id;
        entry.CreatedAt = existing.CreatedAt;
        entry.UpdatedAt = DateTime.UtcNow;
        await _database.UpdateAsync(entry);
        return entry;
    }

    public static async Task<Tag> AddTagAsync(string name, bool isPrebuilt)
    {
        if (_database is null) throw new InvalidOperationException("Database not initialized.");

        var normalized = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException("Tag name is required.", nameof(name));

        var existing = await _database.Table<Tag>()
            .Where(t => t.Name.ToLower() == normalized.ToLower())
            .FirstOrDefaultAsync();

        if (existing is not null) return existing;

        var tag = new Tag
        {
            Name = normalized,
            IsPrebuilt = isPrebuilt
        };

        await _database.InsertAsync(tag);
        return tag;
    }

    private static DateTime NormalizeDate(DateTime date)
    {
        var local = date.Kind == DateTimeKind.Unspecified ? date : date.ToLocalTime();
        return local.Date;
    }
}
