namespace JournalApp.Models;

public class AnalyticsData
{
    // Core statistics
    public int TotalEntries { get; set; }
    public int TotalWords { get; set; }
    public int AverageWordsPerEntry { get; set; }
    public int EntriesThisMonth { get; set; }
    public int EntriesThisWeek { get; set; }

    // Mood analytics
    public Dictionary<string, int> MoodDistribution { get; set; } = new();
    public string MostFrequentMood { get; set; } = string.Empty;

    // Tag analytics
    public Dictionary<string, int> TagFrequency { get; set; } = new();

    // Category analytics
    public Dictionary<string, double> CategoryPercentages { get; set; } = new();

    // Writing trends
    public List<WordCountTrend> WordCountTrends { get; set; } = new();
}

public class WordCountTrend
{
    public DateTime Date { get; set; }
    public int WordCount { get; set; }
}