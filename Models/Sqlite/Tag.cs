using SQLite;

namespace JournalApp.Models.Sqlite;

[Table("Tag")]
public class Tag
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Name = "IX_Tag_Name", Unique = true)]
    public string Name { get; set; } = "";

    public bool IsPrebuilt { get; set; }

    public string Category { get; set; } = "";
}
