
namespace JournalApp.Services;
using JournalApp.Models;

public class TagCatalog
{
    private readonly AppDataContext _context;

    public TagCatalog(AppDataContext context)
    {
        _context = context;
    }

    public List<Tag> GetUserTags(string userId)
    {
        return _context.Tags
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .ToList();
    }
}
