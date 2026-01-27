namespace JournalApp.Models;

public class StreakInfo
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime? LastEntryDate { get; set; }
    public bool IsActiveToday { get; set; }
    public int TotalEntries { get; set; }
    public List<DateTime> MissedDays { get; set; } = new();  // CHANGED FROM int TO List<DateTime>
}