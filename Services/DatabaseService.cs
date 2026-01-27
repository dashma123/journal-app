using SQLite;
using JournalApp.Models;
using System.Diagnostics;

namespace JournalApp.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection? _database;
    private readonly string _dbPath;
    private bool _initialized = false;
    // Prevents multiple threads from initializing database at the same time
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public DatabaseService()
    {
        try
        {
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, "journal.db");
            Debug.WriteLine($"Database path: {_dbPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in DatabaseService constructor: {ex.Message}");
            throw;
        }
    }

    private async Task InitializeAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (_initialized)
                return;

            Debug.WriteLine("Initializing database...");

            _database = new SQLiteAsyncConnection(_dbPath);
            // Creates required tables
            await _database.CreateTableAsync<JournalEntry>();
            await _database.CreateTableAsync<UserSettings>();

            var settings = await _database.Table<UserSettings>().FirstOrDefaultAsync();
            if (settings == null)
            {
                await _database.InsertAsync(new UserSettings { Id = 1, Theme = "Light" });
            }

            _initialized = true;
            Debug.WriteLine("Database initialized successfully!");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Database initialization error: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }
    
    
// Makes sure database is initialized before any operation
    private async Task EnsureInitialized()
    {
        if (!_initialized)
            await InitializeAsync();
    }

    public async Task<List<JournalEntry>> GetAllEntriesAsync()
    {
        await EnsureInitialized();
        return await _database!.Table<JournalEntry>()
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync();
    }

    public async Task<JournalEntry?> GetEntryByIdAsync(int id)
    {
        await EnsureInitialized();
        return await _database!.Table<JournalEntry>()
            .Where(e => e.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<JournalEntry?> GetEntryByDateAsync(DateTime date)
    {
        await EnsureInitialized();
        var dateOnly = date.Date;
        return await _database!.Table<JournalEntry>()
            .Where(e => e.EntryDate.Date == dateOnly)
            .FirstOrDefaultAsync();
    }

    public async Task<int> InsertEntryAsync(JournalEntry entry)
    {
        await EnsureInitialized();
        entry.CreatedAt = DateTime.Now;
        entry.UpdatedAt = DateTime.Now;
        entry.Category = MoodConfig.GetCategory(entry.PrimaryMood);
        return await _database!.InsertAsync(entry);
    }

    public async Task<int> UpdateEntryAsync(JournalEntry entry)
    {
        await EnsureInitialized();
        entry.UpdatedAt = DateTime.Now;
        entry.Category = MoodConfig.GetCategory(entry.PrimaryMood);
        return await _database!.UpdateAsync(entry);
    }

    public async Task<int> DeleteEntryAsync(int id)
    {
        await EnsureInitialized();
        return await _database!.DeleteAsync<JournalEntry>(id);
    }

    public async Task<List<JournalEntry>> SearchEntriesAsync(string searchTerm)
    {
        await EnsureInitialized();
        searchTerm = searchTerm.ToLowerInvariant();
        var allEntries = await GetAllEntriesAsync();
        return allEntries.Where(e =>
            (e.Title?.ToLowerInvariant().Contains(searchTerm) ?? false) ||
            (e.Content?.ToLowerInvariant().Contains(searchTerm) ?? false)
        ).ToList();
    }

    public async Task<List<JournalEntry>> FilterEntriesAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        List<string>? moods = null,
        List<string>? tags = null)
    {
        await EnsureInitialized();
        var entries = await GetAllEntriesAsync();

        if (startDate.HasValue)
            entries = entries.Where(e => e.EntryDate.Date >= startDate.Value.Date).ToList();

        if (endDate.HasValue)
            entries = entries.Where(e => e.EntryDate.Date <= endDate.Value.Date).ToList();

        if (moods != null && moods.Any())
        {
            entries = entries.Where(e => 
            {
                var entryMoods = e.GetTags();
                return entryMoods.Intersect(moods).Any();
            }).ToList();
        }

        if (tags != null && tags.Any())
        {
            entries = entries.Where(e => 
            {
                var entryTags = e.GetTags();
                return entryTags.Intersect(tags).Any();
            }).ToList();
        }

        return entries;
    }

    public async Task<UserSettings> GetSettingsAsync()
    {
        await EnsureInitialized();
        var settings = await _database!.Table<UserSettings>().FirstOrDefaultAsync();
        return settings ?? new UserSettings { Id = 1, Theme = "Light" };
    }

    public async Task<int> UpdateSettingsAsync(UserSettings settings)
    {
        await EnsureInitialized();
        return await _database!.UpdateAsync(settings);
    }

    public async Task<AnalyticsData> GetAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        await EnsureInitialized();
        var entries = await FilterEntriesAsync(startDate, endDate);
        var analytics = new AnalyticsData();

        var moodsByCategory = entries
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        analytics.MoodDistribution = moodsByCategory;

        var total = entries.Count;
        if (total > 0)
        {
            analytics.CategoryPercentages = moodsByCategory.ToDictionary(
                kvp => kvp.Key,
                kvp => (double)kvp.Value / total * 100
            );
        }

        var allMoods = entries.SelectMany(e => e.GetTags()).ToList();
        if (allMoods.Any())
        {
            analytics.MostFrequentMood = allMoods
                .GroupBy(m => m)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;
        }

        var allTags = entries.SelectMany(e => e.GetTags()).ToList();
        analytics.TagFrequency = allTags
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToDictionary(g => g.Key, g => g.Count());

        analytics.WordCountTrends = entries
            .OrderBy(e => e.EntryDate)
            .Select(e => new WordCountTrend
            {
                Date = e.EntryDate,
                WordCount = e.WordCount
            })
            .ToList();

        return analytics;
    }
    // Calculates current streak, longest streak, total entries, and missed days
    public async Task<StreakInfo> GetStreakInfoAsync()
    {
        await EnsureInitialized();
        var entries = await GetAllEntriesAsync();
        var entryDates = entries.Select(e => e.EntryDate.Date).Distinct().OrderByDescending(d => d).ToList();

        var streakInfo = new StreakInfo
        {
            TotalEntries = entries.Count
        };

        if (!entryDates.Any())
            return streakInfo;

        var today = DateTime.Today;
        var currentStreak = 0;
        var checkDate = today;

        if (entryDates.Contains(today) || entryDates.Contains(today.AddDays(-1)))
        {
            checkDate = entryDates.Contains(today) ? today : today.AddDays(-1);

            while (entryDates.Contains(checkDate))
            {
                currentStreak++;
                checkDate = checkDate.AddDays(-1);
            }
        }

        streakInfo.CurrentStreak = currentStreak;

        var longestStreak = 0;
        var tempStreak = 1;

        for (int i = 0; i < entryDates.Count - 1; i++)
        {
            if ((entryDates[i] - entryDates[i + 1]).Days == 1)
            {
                tempStreak++;
            }
            else
            {
                longestStreak = Math.Max(longestStreak, tempStreak);
                tempStreak = 1;
            }
        }
        longestStreak = Math.Max(longestStreak, tempStreak);
        streakInfo.LongestStreak = longestStreak;

        var thirtyDaysAgo = today.AddDays(-30);
        var missedDaysList = new List<DateTime>();

        for (var date = thirtyDaysAgo; date <= today; date = date.AddDays(1))
        {
            if (!entryDates.Contains(date) && date < today)
            {
                missedDaysList.Add(date);
            }
        }

        streakInfo.MissedDays = missedDaysList;

        return streakInfo;
    }

    public string GetDatabasePath() => _dbPath;
}