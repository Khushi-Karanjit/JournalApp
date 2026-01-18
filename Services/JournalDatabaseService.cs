using JournalApp.Data;

namespace JournalApp.Services;

public class JournalDatabaseService
{
    private readonly Task _initTask;

    public JournalDatabaseService()
    {
        _initTask = JournalDatabase.InitAsync();
    }

    public Task EnsureInitializedAsync()
    {
        return _initTask;
    }
}
