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

            await _database.CreateTableAsync<DbPlaceholder>();

            _initialized = true;
        }
        finally
        {
            InitLock.Release();
        }
    }

    public class DbPlaceholder
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
    }
}
