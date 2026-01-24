namespace JournalApp.Models;

public class Tag
{
    public Guid TagId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";

    public string Name { get; set; } = "";
    public bool IsPredefined { get; set; }
}
