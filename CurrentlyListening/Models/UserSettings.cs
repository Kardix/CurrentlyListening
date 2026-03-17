using System.Globalization;

namespace CurrentlyListening.Models;

public class UserSettings
{
    public string OutputFilePath { get; set; }
    public string Language { get; set; }
    public bool Artist { get; set; }
    public bool Song { get; set; }
    public bool Duration { get; set; }
    public bool CloseToTray { get; set; }
    public bool StartMinimized { get; set; }
    public bool CheckForUpdates { get; set; }
    public int TrailingSpacesCount { get; set; }
}