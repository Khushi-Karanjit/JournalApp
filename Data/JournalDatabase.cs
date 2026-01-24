using JournalApp.Models.Sqlite;
using Microsoft.Maui.Storage;
using SQLite;
using User = JournalApp.Models.User;

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
            await _database.CreateTableAsync<User>(); // New User table

            await EnsureJournalEntryCategoryColumnAsync();
            await EnsureTagCategoryColumnAsync();

            await SeedAsync();

            _initialized = true;
        }
        finally
        {
            InitLock.Release();
        }
    }

    public static async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        await InitAsync();
        if (_database is null) throw new InvalidOperationException("Database not initialized.");
        return _database;
    }

    public static async Task<User?> GetUserAsync()
    {
        await InitAsync();
        // Since single user app, return the first user
        return await _database!.Table<User>().FirstOrDefaultAsync();
    }

    public static async Task SaveUserAsync(User user)
    {
        await InitAsync();
        if (string.IsNullOrEmpty(user.Id)) user.Id = Guid.NewGuid().ToString();
        
        var existing = await _database!.Table<User>().Where(u => u.Id == user.Id).FirstOrDefaultAsync();
        if (existing == null)
        {
             // Check if ANY user exists (to prevent multiples if ID is different)
             var anyUser = await _database.Table<User>().FirstOrDefaultAsync();
             if (anyUser != null)
             {
                 // Update the existing single user instead of inserting a new one
                 user.Id = anyUser.Id;
                 await _database.UpdateAsync(user);
             }
             else
             {
                 await _database.InsertAsync(user);
             }
        }
        else
        {
            await _database.UpdateAsync(user);
        }
    }

    public static DateTime NormalizeEntryDate(DateTime date)
    {
        var local = date.Kind == DateTimeKind.Unspecified ? date : date.ToLocalTime();
        return local.Date;
    }

    public static async Task<JournalEntry> UpsertEntryForDayAsync(JournalEntry entry)
    {
        if (_database is null) throw new InvalidOperationException("Database not initialized.");

        entry.EntryDate = NormalizeEntryDate(entry.EntryDate);

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

    public static async Task<Tag> AddTagAsync(string name, bool isPrebuilt, string category)
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
            IsPrebuilt = isPrebuilt,
            Category = category ?? ""
        };

        await _database.InsertAsync(tag);
        return tag;
    }

    private static async Task SeedAsync()
    {
        if (_database is null) throw new InvalidOperationException("Database not initialized.");

        await SeedMoodsAsync();
        await SeedTagsAsync();
        await SeedTagCategoriesAsync();
    }

    private static async Task SeedMoodsAsync()
    {
        if (_database is null) throw new InvalidOperationException("Database not initialized.");

        var moods = new (string Name, string Category)[]
        {
            ("Happy", "Positive"),
            ("Excited", "Positive"),
            ("Relaxed", "Positive"),
            ("Grateful", "Positive"),
            ("Confident", "Positive"),

            ("Calm", "Neutral"),
            ("Thoughtful", "Neutral"),
            ("Curious", "Neutral"),
            ("Nostalgic", "Neutral"),
            ("Bored", "Neutral"),

            ("Sad", "Negative"),
            ("Angry", "Negative"),
            ("Stressed", "Negative"),
            ("Lonely", "Negative"),
            ("Anxious", "Negative")
        };

        foreach (var mood in moods)
        {
            var existing = await _database.Table<Mood>()
                .Where(m => m.Name == mood.Name && m.Category == mood.Category)
                .FirstOrDefaultAsync();

            if (existing is not null) continue;

            await _database.InsertAsync(new Mood
            {
                Name = mood.Name,
                Category = mood.Category
            });
        }
    }

    private static async Task SeedTagsAsync()
    {
        if (_database is null) throw new InvalidOperationException("Database not initialized.");

        var tags = new[]
        {
            "Work", "Career", "Studies", "Family", "Friends", "Relationships",
            "Health", "Fitness", "Personal Growth", "Self-care", "Hobbies",
            "Travel", "Nature", "Finance", "Spirituality", "Birthday",
            "Holiday", "Vacation", "Celebration", "Exercise", "Reading",
            "Writing", "Cooking", "Meditation", "Yoga", "Music", "Shopping",
            "Parenting", "Projects", "Planning", "Reflection"
        };

        foreach (var name in tags)
        {
            var normalized = name.Trim();
            var existing = await _database.Table<Tag>()
                .Where(t => t.Name.ToLower() == normalized.ToLower())
                .FirstOrDefaultAsync();

            if (existing is not null) continue;

            await _database.InsertAsync(new Tag
            {
                Name = normalized,
                IsPrebuilt = true,
                Category = MapTagCategory(normalized)
            });
        }
    }

    private static async Task EnsureTagCategoryColumnAsync()
    {
        if (_database is null) throw new InvalidOperationException("Database not initialized.");

        var columns = await _database.QueryAsync<TableInfoRow>("PRAGMA table_info(Tag)");
        var hasCategory = columns.Any(c => c.Name.Equals("Category", StringComparison.OrdinalIgnoreCase));
        if (hasCategory) return;

        await _database.ExecuteAsync("ALTER TABLE Tag ADD COLUMN Category TEXT");
    }

    private static async Task SeedTagCategoriesAsync()
    {
        if (_database is null) throw new InvalidOperationException("Database not initialized.");

        var tags = await _database.Table<Tag>().ToListAsync();
        foreach (var tag in tags)
        {
            if (!string.IsNullOrWhiteSpace(tag.Category)) continue;
            tag.Category = MapTagCategory(tag.Name);
            await _database.UpdateAsync(tag);
        }
    }

    private static string MapTagCategory(string name)
    {
        return name switch
        {
            "Work" or "Career" or "Studies" or "Projects" or "Planning" or "Reflection" => "Work & Study",
            "Family" or "Friends" or "Relationships" or "Parenting" => "Relationships",
            "Health" or "Fitness" or "Self-care" or "Exercise" or "Yoga" or "Meditation" => "Health",
            "Travel" or "Nature" or "Vacation" or "Holiday" or "Birthday" or "Celebration" => "Travel & Events",
            "Hobbies" or "Reading" or "Writing" or "Cooking" or "Music" or "Shopping" => "Lifestyle",
            "Finance" or "Spirituality" or "Personal Growth" => "Wellbeing",
            _ => "Custom"
        };
    }

    private static async Task EnsureJournalEntryCategoryColumnAsync()
    {
        if (_database is null) throw new InvalidOperationException("Database not initialized.");

        var columns = await _database.QueryAsync<TableInfoRow>("PRAGMA table_info(JournalEntry)");
        var hasCategory = columns.Any(c => c.Name.Equals("Category", StringComparison.OrdinalIgnoreCase));
        if (hasCategory) return;

        await _database.ExecuteAsync("ALTER TABLE JournalEntry ADD COLUMN Category TEXT");
    }

    private sealed class TableInfoRow
    {
        public string Name { get; set; } = "";
    }
}
