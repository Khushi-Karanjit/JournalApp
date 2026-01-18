using JournalApp.Models;

namespace JournalApp.Services;

public class MoodCatalog
{
    public List<MoodGroup> GetMoodsByCategory()
    {
        return new List<MoodGroup>
        {
            new(MoodCategory.Positive, new List<Mood>
            {
                Mood.Happy,
                Mood.Excited,
                Mood.Relaxed,
                Mood.Grateful,
                Mood.Confident
            }),
            new(MoodCategory.Neutral, new List<Mood>
            {
                Mood.Calm,
                Mood.Thoughtful,
                Mood.Curious,
                Mood.Nostalgic,
                Mood.Bored
            }),
            new(MoodCategory.Negative, new List<Mood>
            {
                Mood.Sad,
                Mood.Angry,
                Mood.Stressed,
                Mood.Lonely,
                Mood.Anxious
            })
        };
    }

    public List<Mood> GetAvailableMoods()
    {
        return GetMoodsByCategory()
            .SelectMany(g => g.Moods)
            .ToList();
    }

    public record MoodGroup(MoodCategory Category, List<Mood> Moods);
}
