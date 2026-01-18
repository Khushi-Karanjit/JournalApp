using SQLite;

namespace JournalApp.Models.Sqlite;

[Table("EntryTag")]
public class EntryTag
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Name = "IX_EntryTag", Order = 1, Unique = true)]
    public int EntryId { get; set; }

    [Indexed(Name = "IX_EntryTag", Order = 2, Unique = true)]
    public int TagId { get; set; }
}
