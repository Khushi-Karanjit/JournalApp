namespace JournalApp.Models;

public class DiaryEntry
{
    public Guid EntryId { get; set; }
    public string OwnerId { get; set; } = "";
    public DateOnly EntryDay { get; set; }

    public string Title { get; set; } = "";
    public string Content { get; set; } = "";

    // ONLY mood system
    public List<Mood> Moods { get; set; } = new();

    public List<Tag> Tags { get; set; } = new();

    public DateTime CreatedOn { get; set; }
    public DateTime LastUpdatedOn { get; set; }

    public int WordCount =>
        string.IsNullOrWhiteSpace(Content)
            ? 0
            : Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
}
