using JournalApp.Models;
using System.Text;

namespace JournalApp.Services;

public class JournalService
{
    private readonly List<JournalEntry> _entries = new();

    // BASIC CRUD

    public Task<List<JournalEntry>> GetAllEntriesAsync()
        => Task.FromResult(_entries.OrderByDescending(e => e.EntryDate).ToList());

    public Task<JournalEntry?> GetEntryByIdAsync(int id)
        => Task.FromResult(_entries.FirstOrDefault(e => e.Id == id));

    public Task AddEntryAsync(JournalEntry entry)
    {
        entry.Id = _entries.Any() ? _entries.Max(e => e.Id) + 1 : 1;
        entry.CreatedAt = DateTime.Now;
        entry.UpdatedAt = DateTime.Now;
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task SaveEntryAsync(JournalEntry entry)
    {
        if (entry.Id == 0)
            return AddEntryAsync(entry);

        return UpdateEntryAsync(entry);
    }

    public Task UpdateEntryAsync(JournalEntry entry)
    {
        var existing = _entries.FirstOrDefault(e => e.Id == entry.Id);
        if (existing == null) return Task.CompletedTask;

        existing.Title = entry.Title;
        existing.Content = entry.Content;
        existing.PrimaryMood = entry.PrimaryMood;
        existing.SecondaryMood1 = entry.SecondaryMood1;
        existing.SecondaryMood2 = entry.SecondaryMood2;
        existing.SetTags(entry.GetTags());
        existing.EntryDate = entry.EntryDate;
        existing.UpdatedAt = DateTime.Now;

        return Task.CompletedTask;
    }

    public Task DeleteEntryAsync(int id)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == id);
        if (entry != null) _entries.Remove(entry);
        return Task.CompletedTask;
    }

    //  SEARCH

    public Task<List<JournalEntry>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return GetAllEntriesAsync();

        query = query.ToLowerInvariant();

        var results = _entries
            .Where(e =>
                (e.Title?.ToLowerInvariant().Contains(query) ?? false) ||
                (e.Content?.ToLowerInvariant().Contains(query) ?? false) ||
                (e.PrimaryMood?.ToLowerInvariant().Contains(query) ?? false) ||
                (e.GetTags().Any(t => t.ToLowerInvariant().Contains(query))))
            .OrderByDescending(e => e.EntryDate)
            .ToList();

        return Task.FromResult(results);
    }

    // CALENDAR 

    public Task<Dictionary<DateTime, JournalEntry>> GetEntriesByMonthAsync(int year, int month)
    {
        var result = _entries
            .Where(e => e.EntryDate.Year == year && e.EntryDate.Month == month)
            .ToDictionary(e => e.EntryDate.Date, e => e);

        return Task.FromResult(result);
    }

    //  ANALYTICS 

    public Task<AnalyticsData> GetAnalyticsAsync(DateTime? start = null, DateTime? end = null)
    {
        // Filter entries by date range if provided
        var filteredEntries = _entries.AsEnumerable();
        
        if (start.HasValue)
            filteredEntries = filteredEntries.Where(e => e.EntryDate.Date >= start.Value.Date);
            
        if (end.HasValue)
            filteredEntries = filteredEntries.Where(e => e.EntryDate.Date <= end.Value.Date);

        var entries = filteredEntries.ToList();
        var data = new AnalyticsData();

        // Basic stats
        data.TotalEntries = entries.Count;
        data.TotalWords = entries.Sum(e => e.WordCount);
        data.AverageWordsPerEntry = entries.Any() ? data.TotalWords / entries.Count : 0;

        data.MoodDistribution = entries
            .Where(e => !string.IsNullOrWhiteSpace(e.PrimaryMood))
            .GroupBy(e => MoodConfig.GetCategory(e.PrimaryMood))  // ← FIX: Use MoodConfig.GetCategory()
            .ToDictionary(g => g.Key, g => g.Count());

        data.MostFrequentMood = entries
            .Where(e => !string.IsNullOrWhiteSpace(e.PrimaryMood))
            .GroupBy(e => e.PrimaryMood)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key ?? "N/A";

        // Category percentages
        if (entries.Any())
        {
            var total = entries.Count;
            data.CategoryPercentages = data.MoodDistribution
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => (double)kvp.Value / total * 100
                );
        }

        // Tag frequency
        data.TagFrequency = entries
            .SelectMany(e => e.GetTags())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .GroupBy(t => t)
            .ToDictionary(g => g.Key, g => g.Count());

        // Word count trends
        data.WordCountTrends = entries
            .OrderBy(e => e.EntryDate)
            .Select(e => new WordCountTrend
            {
                Date = e.EntryDate,
                WordCount = e.WordCount
            })
            .ToList();

        return Task.FromResult(data);
    }

    //  FILTER

    public Task<List<JournalEntry>> FilterAsync(DateTime? start, DateTime? end)
    {
        var query = _entries.AsQueryable();

        if (start.HasValue)
            query = query.Where(e => e.EntryDate.Date >= start.Value.Date);

        if (end.HasValue)
            query = query.Where(e => e.EntryDate.Date <= end.Value.Date);

        var result = query
            .OrderByDescending(e => e.EntryDate)
            .ToList();

        return Task.FromResult(result);
    }

    //  EXPORT 

    public Task<string> ExportToMarkdownAsync()
    {
        var sb = new StringBuilder();

        foreach (var e in _entries.OrderBy(e => e.EntryDate))
        {
            sb.AppendLine("# " + e.Title);
            sb.AppendLine("Date: " + e.EntryDate.ToString("yyyy-MM-dd"));
            sb.AppendLine("Mood: " + e.PrimaryMood);
            sb.AppendLine();

            var tags = e.GetTags();
            if (tags.Any())
                sb.AppendLine("Tags: " + string.Join(", ", tags));

            sb.AppendLine();
            sb.AppendLine(e.Content);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        return Task.FromResult(sb.ToString());
    }

    //ADDITIONAL API 

    public Task<bool> HasEntryForDateAsync(DateTime date)
        => Task.FromResult(_entries.Any(e => e.EntryDate.Date == date.Date));

    public Task<JournalEntry?> GetEntryForDateAsync(DateTime date)
        => Task.FromResult(_entries.FirstOrDefault(e => e.EntryDate.Date == date.Date));

    public Task<StreakInfo> GetStreakInfoAsync()
    {
        if (!_entries.Any())
            return Task.FromResult(new StreakInfo
            {
                CurrentStreak = 0,
                LongestStreak = 0,
                TotalEntries = 0,
                MissedDays = new List<DateTime>()
            });

        var dates = _entries
            .Select(e => e.EntryDate.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        // Calculate current streak (from today backwards)
        int currentStreak = 0;
        var today = DateTime.Today;
        var checkDate = today;

        while (dates.Contains(checkDate))
        {
            currentStreak++;
            checkDate = checkDate.AddDays(-1);
        }

        // Calculate longest streak
        var allDates = dates.OrderBy(d => d).ToList();
        int longestStreak = 1;
        int tempStreak = 1;

        for (int i = 1; i < allDates.Count; i++)
        {
            if ((allDates[i] - allDates[i - 1]).Days == 1)
            {
                tempStreak++;
                longestStreak = Math.Max(longestStreak, tempStreak);
            }
            else
            {
                tempStreak = 1;
            }
        }

        // Calculate missed days in last 30 days
        var missedDays = new List<DateTime>();
        var thirtyDaysAgo = today.AddDays(-30);
        
        for (var date = thirtyDaysAgo; date <= today; date = date.AddDays(1))
        {
            if (!dates.Contains(date))
            {
                missedDays.Add(date);
            }
        }

        return Task.FromResult(new StreakInfo
        {
            CurrentStreak = currentStreak,
            LongestStreak = longestStreak,
            TotalEntries = _entries.Count,
            MissedDays = missedDays
        });
    }

    // MOOD & TAG ANALYTICS 
    
    public Task<int> GetMoodStreak(string mood)
    {
        var entries = _entries
            .Where(e => e.PrimaryMood == mood)
            .OrderByDescending(e => e.EntryDate)
            .Select(e => e.EntryDate.Date)
            .ToList();

        if (!entries.Any())
            return Task.FromResult(0);

        int streak = 1;
        var yesterday = DateTime.Today.AddDays(-1);
        
        // Check if the most recent entry is today or yesterday
        if (entries[0] != DateTime.Today && entries[0] != yesterday)
            return Task.FromResult(0);

        for (int i = 1; i < entries.Count; i++)
        {
            var diff = (entries[i - 1] - entries[i]).Days;
            if (diff == 1)
                streak++;
            else
                break;
        }

        return Task.FromResult(streak);
    }

    public Task<int> GetTagUsageCount(string tag)
    {
        var count = _entries.Count(e => e.GetTags().Contains(tag));
        return Task.FromResult(count);
    }
}