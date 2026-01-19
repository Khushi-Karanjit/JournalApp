using SQLite;

namespace JournalApp.Models.Sqlite;

[Table("JournalEntry")]
public class JournalEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Name = "IX_JournalEntry_EntryDate", Unique = true)]
    public DateTime EntryDate { get; set; }

    public string? Title { get; set; }

    public string Content { get; set; } = "";

    public int PrimaryMoodId { get; set; }

    public int? SecondaryMood1Id { get; set; }

    public int? SecondaryMood2Id { get; set; }

    public string Category { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
