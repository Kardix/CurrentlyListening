using System.Globalization;
using System.IO;
using Newtonsoft.Json;

namespace CurrentlyListening.Properties;

public static class Settings
{
    private static string _langCode = "en"; // default value
    private static string _outputFilePath = "trackinfo.txt"; // Default value
    private static bool _showArtist = true;
    private static bool _showTitle = true;
    private static bool _showDuration = true;


    // Property to get and set the file path
    public static string OutputFilePath
    {
        get { return _outputFilePath; }
        set
        {
            if (_outputFilePath != value)
            {
                _outputFilePath = value;
                SaveSettings(); // Optionally save settings here if you want to persist them.
            }
        }
    }
    
    // Property to get and set the file path
    public static bool ShowArtist
    {
        get { return _showArtist; }
        set
        {
            if (_showArtist != value)
            {
                _showArtist = value;
                SaveSettings(); // Optionally save settings here if you want to persist them.
            }
        }
    }
    
    // Property to get and set the file path
    public static bool ShowTitle
    {
        get { return _showTitle; }
        set
        {
            if (_showTitle != value)
            {
                _showTitle = value;
                SaveSettings(); // Optionally save settings here if you want to persist them.
            }
        }
    }
    
    // Property to get and set the file path
    public static bool ShowDuration
    {
        get { return _showDuration; }
        set
        {
            if (_showDuration != value)
            {
                _showDuration = value;
                SaveSettings(); // Optionally save settings here if you want to persist them.
            }
        }
    }
    
    // Property to get and set the file path
    public static string LangCode
    {
        get { return _langCode; }
        set
        {
            if (_langCode != value)
            {
                _langCode = value;
                SaveSettings(); // Optionally save settings here if you want to persist them.
            }
        }
    }
    
    // Method to save settings (you can use JSON or XML serialization for persistence)
    public static void SaveSettings()
    {
        // For example, save settings to a file or use System.IO to save it locally.
        // Here you can use a simple file-based persistence or JSON as an alternative.
        string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SpotifyRetranslator", "settings.json");
        var settings = new { OutputFilePath = _outputFilePath, Language = _langCode, Artist = _showArtist, Song = _showTitle,
            Duration = _showDuration };
        File.WriteAllText(settingsPath, JsonConvert.SerializeObject(settings));
    }

    // Method to load settings (read from a file)
    public static void LoadSettings()
    {
        string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SpotifyRetranslator", "settings.json");
        if (File.Exists(settingsPath))
        {
            var settingsJson = File.ReadAllText(settingsPath);
            var settings = JsonConvert.DeserializeObject<dynamic>(settingsJson);
            _outputFilePath = settings?.OutputFilePath ?? "trackinfo.txt"; // Default fallback
            _showArtist = settings?.Artist ?? true;
            _showTitle = settings?.Song ?? true;
            _showDuration = settings?.Duration ?? true;
            _langCode = settings?.Language ?? CultureInfo.CurrentUICulture;
        }
    }
}