namespace JournalApp.Models;

public class User
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Default User";
}
