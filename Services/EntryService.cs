using JournalApp.Data;
using JournalApp.Models.Sqlite;

namespace JournalApp.Services;

public class EntryService
{
    public async Task<JournalEntry?> GetByDateAsync(DateTime date)
    {
        var db = await JournalDatabase.GetConnectionAsync();
        var normalized = JournalDatabase.NormalizeEntryDate(date);

        return await db.Table<JournalEntry>()
            .Where(e => e.EntryDate == normalized)
            .FirstOrDefaultAsync();
    }

    public Task<JournalEntry> UpsertAsync(JournalEntry entry)
    {
        if (entry is null) throw new ArgumentNullException(nameof(entry));
        return JournalDatabase.UpsertEntryForDayAsync(entry);
    }

    public async Task<bool> DeleteByDateAsync(DateTime date)
    {
        var db = await JournalDatabase.GetConnectionAsync();
        var normalized = JournalDatabase.NormalizeEntryDate(date);

        var existing = await db.Table<JournalEntry>()
            .Where(e => e.EntryDate == normalized)
            .FirstOrDefaultAsync();

        if (existing is null) return false;
        await db.DeleteAsync(existing);
        return true;
    }

    public async Task<List<JournalEntry>> GetPagedAsync(int page, int pageSize)
    {
        var db = await JournalDatabase.GetConnectionAsync();
        var pageIndex = Math.Max(1, page);
        var size = Math.Max(1, pageSize);
        var skip = (pageIndex - 1) * size;

        return await db.Table<JournalEntry>()
            .OrderByDescending(e => e.EntryDate)
            .Skip(skip)
            .Take(size)
            .ToListAsync();
    }

    public async Task<List<JournalEntry>> GetEntriesInRangeAsync(DateTime startDate, DateTime endDate)
    {
        var db = await JournalDatabase.GetConnectionAsync();
        var start = JournalDatabase.NormalizeEntryDate(startDate);
        var end = JournalDatabase.NormalizeEntryDate(endDate);

        return await db.Table<JournalEntry>()
            .Where(e => e.EntryDate >= start && e.EntryDate <= end)
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync();
    }

    public async Task<int> GetEntryCountAsync()
    {
        var db = await JournalDatabase.GetConnectionAsync();
        return await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM JournalEntry");
    }

    public async Task<HashSet<DateTime>> GetEntryDatesInRangeAsync(DateTime startDate, DateTime endDate)
    {
        var db = await JournalDatabase.GetConnectionAsync();
        var start = JournalDatabase.NormalizeEntryDate(startDate);
        var end = JournalDatabase.NormalizeEntryDate(endDate);

        var rows = await db.Table<JournalEntry>()
            .Where(e => e.EntryDate >= start && e.EntryDate <= end)
            .ToListAsync();

        return rows
            .Select(e => JournalDatabase.NormalizeEntryDate(e.EntryDate))
            .ToHashSet();
    }

    public async Task<HashSet<DateTime>> GetAllEntryDatesAsync()
    {
        var db = await JournalDatabase.GetConnectionAsync();
        var rows = await db.QueryAsync<EntryDateRow>("SELECT EntryDate FROM JournalEntry");

        return rows
            .Select(r => JournalDatabase.NormalizeEntryDate(r.EntryDate))
            .ToHashSet();
    }

    public async Task<int> GetSearchCountAsync(
        string? query,
        DateTime? startDate,
        DateTime? endDate,
        List<int>? moodIds,
        List<int>? tagIds)
    {
        var db = await JournalDatabase.GetConnectionAsync();

        var (whereSql, parameters) = BuildSearchWhere(
            query,
            startDate,
            endDate,
            moodIds,
            tagIds);

        var sql = "SELECT COUNT(*) FROM JournalEntry" + whereSql;
        return await db.ExecuteScalarAsync<int>(sql, parameters.ToArray());
    }

    public async Task<List<JournalEntry>> SearchAsync(
        string? query,
        DateTime? startDate,
        DateTime? endDate,
        List<int>? moodIds,
        List<int>? tagIds,
        int page,
        int pageSize,
        string? sortOption)
    {
        var db = await JournalDatabase.GetConnectionAsync();

        var (whereSql, parameters) = BuildSearchWhere(
            query,
            startDate,
            endDate,
            moodIds,
            tagIds);

        var orderBy = sortOption switch
        {
            "date_asc" => " ORDER BY EntryDate ASC",
            "title_asc" => " ORDER BY Title ASC",
            "title_desc" => " ORDER BY Title DESC",
            _ => " ORDER BY EntryDate DESC"
        };

        var pageIndex = Math.Max(1, page);
        var size = Math.Max(1, pageSize);
        var skip = (pageIndex - 1) * size;

        var sql = "SELECT * FROM JournalEntry"
            + whereSql
            + orderBy
            + " LIMIT ? OFFSET ?";

        parameters.Add(size);
        parameters.Add(skip);

        return await db.QueryAsync<JournalEntry>(sql, parameters.ToArray());
    }

    private static (string WhereSql, List<object> Parameters) BuildSearchWhere(
        string? query,
        DateTime? startDate,
        DateTime? endDate,
        List<int>? moodIds,
        List<int>? tagIds)
    {
        var clauses = new List<string>();
        var parameters = new List<object>();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var like = "%" + query.Trim() + "%";
            clauses.Add("(Title LIKE ? OR Content LIKE ?)");
            parameters.Add(like);
            parameters.Add(like);
        }

        if (startDate.HasValue)
        {
            clauses.Add("EntryDate >= ?");
            parameters.Add(JournalDatabase.NormalizeEntryDate(startDate.Value));
        }

        if (endDate.HasValue)
        {
            clauses.Add("EntryDate <= ?");
            parameters.Add(JournalDatabase.NormalizeEntryDate(endDate.Value));
        }

        if (moodIds is { Count: > 0 })
        {
            var distinctMoodIds = moodIds.Distinct().ToList();
            var placeholders = string.Join(",", distinctMoodIds.Select(_ => "?"));
            clauses.Add(
                "(PrimaryMoodId IN (" + placeholders + ") " +
                "OR SecondaryMood1Id IN (" + placeholders + ") " +
                "OR SecondaryMood2Id IN (" + placeholders + "))");

            AddIntParameters(parameters, distinctMoodIds);
            AddIntParameters(parameters, distinctMoodIds);
            AddIntParameters(parameters, distinctMoodIds);
        }

        if (tagIds is { Count: > 0 })
        {
            var distinctTagIds = tagIds.Distinct().ToList();
            var placeholders = string.Join(",", distinctTagIds.Select(_ => "?"));

            clauses.Add(
                "Id IN (" +
                "SELECT EntryId FROM EntryTag " +
                "WHERE TagId IN (" + placeholders + ") " +
                "GROUP BY EntryId " +
                "HAVING COUNT(DISTINCT TagId) = ?)");

            AddIntParameters(parameters, distinctTagIds);
            parameters.Add(distinctTagIds.Count);
        }

        if (clauses.Count == 0)
            return ("", parameters);

        return (" WHERE " + string.Join(" AND ", clauses), parameters);
    }

    private static void AddIntParameters(List<object> parameters, IEnumerable<int> values)
    {
        foreach (var value in values)
            parameters.Add(value);
    }

    private sealed class EntryDateRow
    {
        public DateTime EntryDate { get; set; }
    }
}
