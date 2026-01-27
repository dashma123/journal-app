namespace JournalApp.Models;

public static class MoodConfig
{
    public static readonly string[] AllMoods = 
    {
        "Happy", "Sad", "Angry", "Anxious", "Excited", 
        "Calm", "Stressed", "Grateful", "Tired", "Energetic",
        "Confused", "Confident", "Lonely", "Content", "Frustrated"
    };
    
    // Add MoodCategories for EntryForm
    public static readonly Dictionary<string, string[]> MoodCategories = new()
    {
        { "Positive", new[] { "Happy", "Excited", "Grateful", "Content", "Confident", "Energetic" } },
        { "Negative", new[] { "Sad", "Angry", "Anxious", "Stressed", "Lonely", "Frustrated" } },
        { "Neutral", new[] { "Calm", "Tired", "Confused" } }
    };
    
    public static string GetCategory(string mood)
    {
        return mood switch
        {
            "Happy" or "Excited" or "Grateful" or "Content" or "Confident" or "Energetic" => "Positive",
            "Sad" or "Angry" or "Anxious" or "Stressed" or "Lonely" or "Frustrated" => "Negative",
            "Calm" or "Tired" or "Confused" => "Neutral",
            _ => "General"
        };
    }
    
    public static string GetEmoji(string mood)
    {
        return mood switch
        {
            "Happy" => "😊",
            "Sad" => "😢",
            "Angry" => "😠",
            "Anxious" => "😰",
            "Excited" => "🤩",
            "Calm" => "😌",
            "Stressed" => "😫",
            "Grateful" => "🙏",
            "Tired" => "😴",
            "Energetic" => "⚡",
            "Confused" => "😕",
            "Confident" => "😎",
            "Lonely" => "😔",
            "Content" => "😊",
            "Frustrated" => "😤",
            _ => "😐"
        };
    }
}

// Extension method for string
public static class StringExtensions
{
    public static string ToLowerInvariant(this string str)
    {
        return str?.ToLowerInvariant() ?? string.Empty;
    }
}