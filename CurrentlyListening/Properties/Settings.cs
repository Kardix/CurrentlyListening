using System.Globalization;
using System.IO;
using Newtonsoft.Json;

namespace CurrentlyListening.Properties;

public static class Settings
{
    private static string _langCode = "en"; // default value
    private static string _outputFilePath = "trackinfo.txt"; // Default value

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
        var settings = new { OutputFilePath = _outputFilePath, Language = _langCode };
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
            _langCode = settings?.Language ?? CultureInfo.CurrentUICulture;
        }
    }
}