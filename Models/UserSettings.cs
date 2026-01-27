using SQLite;

namespace JournalApp.Models;

public class UserSettings
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    
    // Selected app theme (Light / Dark)
    public string Theme { get; set; } = "Light";
    
    public bool NotificationsEnabled { get; set; } = true;
    
    public string NotificationTime { get; set; } = "20:00";
    
    public bool AutoBackup { get; set; } = false;
    
    public int BackupFrequency { get; set; } = 7;
    
    public string ExportFormat { get; set; } = "PDF";
    
    public bool ShowMoodReminder { get; set; } = true;
    
    public string DefaultMood { get; set; } = "Neutral";
    
    public bool IsPasswordEnabled { get; set; } = false;
    
    // Stored hashed password (null if password protection is disabled)
    public string? PasswordHash { get; set; }
}