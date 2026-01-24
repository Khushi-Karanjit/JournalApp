

using JournalApp.Models;

namespace JournalApp.Services;

public class AppDataContext
{
    public User ActiveUser { get; } = new User { Username = "Journal User" };

    // Milestone 1: list-based storage only
    public List<DiaryEntry> Entries { get; } = new();
    public List<Tag> Tags { get; } = new();

    public AppDataContext()
    {
        Tags.AddRange(new[]
        {
            new Tag { UserId = ActiveUser.Id, Name = "Work", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Career", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Studies", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Family", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Friends", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Relationships", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Health", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Fitness", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Personal Growth", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Self-care", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Hobbies", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Travel", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Nature", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Finance", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Spirituality", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Birthday", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Holiday", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Vacation", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Celebration", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Exercise", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Reading", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Writing", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Cooking", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Meditation", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Yoga", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Music", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Shopping", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Parenting", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Projects", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Planning", IsPredefined = true },
            new Tag { UserId = ActiveUser.Id, Name = "Reflection", IsPredefined = true }
        });
    }
}
