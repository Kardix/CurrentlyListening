using System.Globalization;
using System.IO;
using CurrentlyListening.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CurrentlyListening.Properties;

public static class AppSettings
{
    public static readonly string DefaultOutputFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CurrentlyListening", "trackinfo.txt");

    private static string _langCode = "en"; // default value
    private static string _outputFilePath = DefaultOutputFilePath;
    private static bool _showArtist = true;
    private static bool _showTitle = true;
    private static bool _showDuration = true;
    private static bool _closeToTray = true;
    private static bool _startMinimized = false;
    private static bool _checkForUpdates = true;
    private static bool _showPopupOnFail = true;
    private static string _closedText = "";
    public static int _trailingSpacesCount { get; set; }


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
    
    // Property to get and set the app close functionality 
    public static bool CloseToTray
    {
        get { return _closeToTray; }
        set
        {
            if (_closeToTray != value)
            {
                _closeToTray = value;
                SaveSettings(); // Optionally save settings here if you want to persist them.
            }
        }
    }
    
    // Property to get and set the app close functionality 
    public static bool StartMinimized
    {
        get { return _startMinimized; }
        set
        {
            if (_startMinimized != value)
            {
                _startMinimized = value;
                SaveSettings();
            }
        }
    }
    
    public static bool EnsureOutputFileIsWritable()
    {
        var path = Path.IsPathFullyQualified(_outputFilePath)
            ? _outputFilePath
            : Path.Combine(AppContext.BaseDirectory, _outputFilePath);

        var fellBack = false;
        if (!CanWriteOutputFile(path))
        {
            path = DefaultOutputFilePath;
            CanWriteOutputFile(path);
            fellBack = true;
        }

        if (path != _outputFilePath)
        {
            _outputFilePath = path;
            TrySaveSettings();
        }

        return fellBack;
    }

    public static bool CanWriteOutputFile(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite)) { }
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    // Returns a valid CultureInfo for the configured language, falling back to
    // English if the stored code is null, empty, or not a recognized culture.
    // Prevents CultureNotFoundException from crashing startup on user machines.
    public static CultureInfo GetCulture()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_langCode))
                return CultureInfo.GetCultureInfo("en");
            return CultureInfo.GetCultureInfo(_langCode);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo("en");
        }
    }

    // Property to get and set the app language
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

    // Not saved here: the Settings window saves when the field loses focus or the window closes.
    public static int TrailingSpacesCount
    {
        get { return _trailingSpacesCount; }
        set { _trailingSpacesCount = value; }
    }
    public static bool CheckForUpdates
    {
        get { return _checkForUpdates; }
        set
        {
            if (_checkForUpdates != value)
            {
                _checkForUpdates = value;
                SaveSettings(); // Optionally save settings here if you want to persist them.
            }
        }
    }

    // Field to determine if the "login failed" should retry the login via browser
    public static bool ShowPopupOnFail
    {
        get { return _showPopupOnFail; }
        set
        {
            if (_showPopupOnFail != value)
            {
                _showPopupOnFail = value;
                SaveSettings();
            }
        }
    }

    // Not saved here: the Settings window saves when the field loses focus or the window closes.
    public static string ClosedText
    {
        get { return _closedText; }
        set { _closedText = value; }
    }

    private static readonly object OutputFileLock = new();
    private static bool _outputClosed;

    public static string WithTrailingSpaces(string text) =>
        string.IsNullOrEmpty(text) ? "" : text + new string(' ', _trailingSpacesCount);

    // Every write to the output file goes through here. Throws IOException or
    // UnauthorizedAccessException like File.WriteAllText. Ignored once the app is closing.
    public static void WriteOutputFile(string text)
    {
        lock (OutputFileLock)
        {
            if (!_outputClosed)
                File.WriteAllText(_outputFilePath, WithTrailingSpaces(text));
        }
    }

    // Called on exit and on a fatal crash (any thread). After this, WriteOutputFile does nothing,
    // so a late timer tick can't put the song back.
    public static void TryWriteClosedText()
    {
        lock (OutputFileLock)
        {
            _outputClosed = true;
            TryWriteFile(_outputFilePath, _closedText);
        }
    }

    // For the old output file after the user picks a new one: tell whoever still reads it.
    public static void TryWriteFile(string path, string text)
    {
        try
        {
            File.WriteAllText(path, WithTrailingSpaces(text));
        }
        catch
        {
            // Best effort: never block exit or a settings change on the output file.
        }
    }

    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpotifyRetranslator");

    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");

    // The previous good settings.json, kept by every save so a broken file can be restored.
    private static readonly string BackupSettingsPath = SettingsPath + ".bak";

    public static void SaveSettings()
    {
        var settings = new{ OutputFilePath = _outputFilePath, Language = _langCode, Artist = _showArtist, Song = _showTitle,
            Duration = _showDuration,  CloseToTray = _closeToTray, StartMinimized = _startMinimized, CheckForUpdates = _checkForUpdates, TrailingSpacesCount = _trailingSpacesCount,
            ShowPopupOnFail = _showPopupOnFail, ClosedText = _closedText };

        Directory.CreateDirectory(SettingsDirectory);

        // Write to a temp file and swap it in, so a crash mid-write can't leave a
        // truncated settings.json behind.
        var tempPath = SettingsPath + ".tmp";
        File.WriteAllText(tempPath, JsonConvert.SerializeObject(settings));
        if (File.Exists(SettingsPath))
            File.Replace(tempPath, SettingsPath, BackupSettingsPath);
        else
            File.Move(tempPath, SettingsPath);
    }

    public static void LoadSettings()
    {
        if (TryApplySettingsFile(SettingsPath))
            return;

        // settings.json is missing or broken: restore the copy kept by the previous save.
        if (TryApplySettingsFile(BackupSettingsPath))
        {
            // Copy rather than save, so the good backup isn't replaced by the broken file.
            try
            {
                File.Copy(BackupSettingsPath, SettingsPath, true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
            return;
        }

        // Broken with no usable backup: keep the current values (the defaults at startup)
        // and write a fresh file over the broken one.
        if (File.Exists(SettingsPath))
            TrySaveSettings();
    }

    // For saves made while starting up: a locked settings.json (antivirus, indexer) must not
    // stop the app from starting. The values still apply for this session, and the next
    // successful save writes them out.
    private static void TrySaveSettings()
    {
        try
        {
            SaveSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    // Reads every value before assigning any, so a bad file can't leave the settings half-loaded.
    private static bool TryApplySettingsFile(string path)
    {
        if (!File.Exists(path))
            return false;

        try
        {
            var settings = JObject.Parse(File.ReadAllText(path));

            var outputFilePath = settings.Value<string>("OutputFilePath");
            var showArtist = settings.Value<bool?>("Artist") ?? true;
            var showTitle = settings.Value<bool?>("Song") ?? true;
            var showDuration = settings.Value<bool?>("Duration") ?? true;
            var langCode = settings.Value<string>("Language") ?? "en";
            var closeToTray = settings.Value<bool?>("CloseToTray") ?? true;
            var startMinimized = settings.Value<bool?>("StartMinimized") ?? false;
            var checkForUpdates = settings.Value<bool?>("CheckForUpdates") ?? true;
            var trailingSpacesCount = Math.Clamp(settings.Value<int?>("TrailingSpacesCount") ?? 0, 0, 50);
            var showPopupOnFail = settings.Value<bool?>("ShowPopupOnFail") ?? true;
            var closedText = settings.Value<string>("ClosedText") ?? "";

            _outputFilePath = string.IsNullOrWhiteSpace(outputFilePath) ? DefaultOutputFilePath : outputFilePath;
            _showArtist = showArtist;
            _showTitle = showTitle;
            _showDuration = showDuration;
            _langCode = langCode;
            _closeToTray = closeToTray;
            _startMinimized = startMinimized;
            _checkForUpdates = checkForUpdates;
            _trailingSpacesCount = trailingSpacesCount;
            _showPopupOnFail = showPopupOnFail;
            _closedText = closedText;
            return true;
        }
        catch (Exception)
        {
            // Unreadable, not JSON, or a value of the wrong type.
            return false;
        }
    }
}