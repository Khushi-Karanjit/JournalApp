
using JournalApp.Models;

namespace JournalApp.Services;

public class AppDataContext
{
    public User ActiveUser { get; } = new User { Name = "Journal User" };

    // Milestone 1: list-based storage only
    public List<DiaryEntry> Entries { get; } = new();
    public List<Tag> Tags { get; } = new();

    public AppDataContext()
    {
        Tags.AddRange(new[]
        {
            new Tag { UserId = ActiveUser.UserId, Name = "Work", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Career", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Studies", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Family", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Friends", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Relationships", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Health", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Fitness", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Personal Growth", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Self-care", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Hobbies", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Travel", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Nature", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Finance", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Spirituality", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Birthday", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Holiday", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Vacation", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Celebration", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Exercise", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Reading", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Writing", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Cooking", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Meditation", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Yoga", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Music", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Shopping", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Parenting", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Projects", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Planning", IsPredefined = true },
            new Tag { UserId = ActiveUser.UserId, Name = "Reflection", IsPredefined = true }
        });
    }
}
