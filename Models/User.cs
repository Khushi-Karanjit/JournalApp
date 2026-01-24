using SQLite;

namespace JournalApp.Models;

public class User
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Username { get; set; } = "Default User";
    public string? PinHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
