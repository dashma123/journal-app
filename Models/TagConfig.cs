namespace JournalApp.Models;

public static class TagConfig
{
    public static readonly string[] SuggestedTags = 
    {
        "Work", "Family", "Friends", "Health", "Exercise",
        "Travel", "Food", "Hobby", "Goals", "Gratitude",
        "Reflection", "Ideas", "Dreams", "Memories", "Plans"
    };
    
    // Alias for EntryForm compatibility
    public static string[] PreBuiltTags => SuggestedTags;
    
    public static string[] GetSuggestedTags()
    {
        return SuggestedTags;
    }
}