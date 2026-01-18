using SQLite;

namespace JournalApp.Models.Sqlite;

[Table("Mood")]
public class Mood
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string Category { get; set; } = "";
}
