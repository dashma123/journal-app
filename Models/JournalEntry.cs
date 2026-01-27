using SQLite;

namespace JournalApp.Models;

public class JournalEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    public DateTime EntryDate { get; set; } = DateTime.Now;
    
    public string Title { get; set; } = string.Empty;
    
    public string Content { get; set; } = string.Empty;
    
    public string PrimaryMood { get; set; } = "Neutral";
    
    public string? SecondaryMood1 { get; set; }
    
    public string? SecondaryMood2 { get; set; }
    
    public string Category { get; set; } = "General";
    
    // Store as string in format "HH:mm" for SQLite compatibility
    public string? WakeUpTimeString { get; set; }
    
    public string? SleepTimeString { get; set; }
    
    public string AllMoods { get; set; } = string.Empty;
    
    public int WordCount { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    
    [Ignore]
    public TimeOnly? WakeUpTime
    {
        get => string.IsNullOrEmpty(WakeUpTimeString) ? null : TimeOnly.Parse(WakeUpTimeString);
        set => WakeUpTimeString = value?.ToString("HH:mm");
    }
    
    [Ignore]
    public TimeOnly? SleepTime
    {
        get => string.IsNullOrEmpty(SleepTimeString) ? null : TimeOnly.Parse(SleepTimeString);
        set => SleepTimeString = value?.ToString("HH:mm");
    }
    
    // For backward compatibility with Date property
    [Ignore]
    public DateTime Date
    {
        get => EntryDate;
        set => EntryDate = value;
    }
    
    // For backward compatibility with Mood property
    [Ignore]
    public string Mood
    {
        get => PrimaryMood;
        set => PrimaryMood = value;
    }
    
    [Ignore]
    public string Tags
    {
        get => AllMoods;
        set => AllMoods = value;
    }
    
    public List<string> GetTags()
    {
        return string.IsNullOrWhiteSpace(AllMoods) 
            ? new List<string>() 
            : AllMoods.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(t => t.Trim())
                  .ToList();
    }
    
    public void SetTags(List<string> tags)
    {
        AllMoods = string.Join(",", tags);
    }
}