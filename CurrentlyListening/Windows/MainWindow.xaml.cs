using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CurrentlyListening.Models;
using CurrentlyListening.Properties;
using CurrentlyListening.Resources;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CurrentlyListening.Windows
{
    public partial class MainWindow
    {
        private const string RedirectUri = "http://127.0.0.1:5000/callback";

        private readonly string _tokenPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SpotifyRetranslator", "tokens.json"
        );

        private string _clientId = "a31d64d9bb5545158552acf47b484cce"; // development spotify key, useless
        private string _clientSecret = "62f7985024ef46e18ca4ff7249e11150"; // development spotify key, useless
        private int _currentSongDuration; // in ms
        private string _currentTrackId;

        private DispatcherTimer _displayTimer = new();
        private IHttpClientFactory _httpClientFactory;
        private int _initialProgressMs;
        private bool _isPlaying = true;
        private string _lastArtist;
        private string _lastTitle;
        private string _outputFilePath = "trackinfo.txt";
        private DateTime _songStartTime;
        private DispatcherTimer _spotifyPollTimer = new();


        private SpotifyToken _tokens;
        string output = "";

        public MainWindow(IHttpClientFactory httpClientFactory)
        {
            InitializeComponent();
            Settings.LoadSettings();
            Thread.CurrentThread.CurrentCulture = new CultureInfo(Settings.LangCode);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.LangCode);
            LanguageSelector.SelectionChanged -= LanguageSelector_SelectionChanged;
            foreach (var item in LanguageSelector.Items)
            {
                if (item is ComboBoxItem comboItem && comboItem.Tag?.ToString() == Settings.LangCode)
                {
                    LanguageSelector.SelectedItem = comboItem;
                    break;
                }
            }

            LanguageSelector.SelectionChanged += LanguageSelector_SelectionChanged;
            NameLabels();
            _outputFilePath = Settings.OutputFilePath;
            _httpClientFactory = httpClientFactory;
            _ = LoadTokens();

            _displayTimer.Interval = TimeSpan.FromSeconds(1);
            _displayTimer.Tick += (s, e) => UpdateDisplayedText();
            _displayTimer.Start();

            _spotifyPollTimer.Interval = TimeSpan.FromSeconds(5);
            _spotifyPollTimer.Tick += async (s, e) => await UpdateTrackInfo();
            _spotifyPollTimer.Start();
        }

        private void NameLabels()
        {
            TrackDisplay.Text = Translations.TRACK_DISPLAY_OPTIONS;
            ArtistCheckbox.Content = Translations.ARTIST;
            TitleCheckbox.Content = Translations.SONG_TITLE;
            TimeCheckbox.Content = Translations.TIME;
            HelpButton.Content = Translations.HELP;
            OutputPreview.Text = Translations.OUTPUT_PREVIEW;
            UpdateDisplay.Content = Translations.UPDATE_DISPLAY;
            LoginToSpotify.Content = Translations.LOGIN_SPOTIFY;
            OutputFileLocation.Content = Translations.SET_OUTPUT_LOCATION;
            UseCustomCredentialsCheckbox.Content = Translations.USE_CUSTOM_CREDENTIALS;
            ClientIdText.Text = Translations.CLIENT_ID;
            ClientSecretText.Text = Translations.CLIENT_SECRET;
            CreatedBy.Text = Translations.CREATED_BY;
        }

        private void SetOutputFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = Translations.SELECT_OUTPUT,
                FileName = "trackinfo.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                Settings.OutputFilePath = dialog.FileName;
                Settings.SaveSettings(); // persist to disk

                MessageBox.Show(Translations.FILE_SAVED, Translations.FILE_SAVED_CAPTION, MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void UpdateDisplay_Click(object sender, RoutedEventArgs e)
        {
            // Simulated data for now
            string artist = "";
            string title = "";
            int progressMs = 0;
            int durationMs = 1;

            string output = "";

            if (ArtistCheckbox.IsChecked == true)
                output += artist;

            if (TitleCheckbox.IsChecked == true)
            {
                if (!string.IsNullOrEmpty(output)) output += " - ";
                output += title;
            }

            if (TimeCheckbox.IsChecked == true)
            {
                if (!string.IsNullOrEmpty(output)) output += " ";
                output += $"{FormatTime(progressMs)} / {FormatTime(durationMs)}";
            }

            OutputBox.Text = output;
            File.WriteAllText(_outputFilePath, output);
        }

        private string FormatTime(int ms) =>
            TimeSpan.FromMilliseconds(ms).ToString(@"m\:ss");

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string state = Guid.NewGuid().ToString("N");
            string scope = "user-read-playback-state user-read-currently-playing";

            if (UseCustomCredentialsCheckbox.IsChecked.Value)
            {
                _clientId = ClientIdTextBox.Text.Trim();
                _clientSecret = ClientSecretTextBox.Text.Trim();
            }

            string authUrl =
                $"https://accounts.spotify.com/authorize?client_id={_clientId}&response_type=code&redirect_uri={HttpUtility.UrlEncode(RedirectUri)}&scope={HttpUtility.UrlEncode(scope)}&state={state}";

            using var listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:5000/callback/");
            listener.Start();

            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

            var context = await listener.GetContextAsync();
            string code = HttpUtility.ParseQueryString(context.Request.Url.Query).Get("code");

            var responseString = $"<html><body>{Translations.AUTH_SUCCESS}</body></html>";
            var buffer = Encoding.UTF8.GetBytes(responseString);
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer);
            context.Response.OutputStream.Close();

            listener.Stop();

            await ExchangeCodeForTokens(code);
        }

        private async Task ExchangeCodeForTokens(string code)
        {
            using var client = _httpClientFactory.CreateClient();
            var postData = new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", RedirectUri),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret)
            };

            var content = new FormUrlEncodedContent(postData);
            var response = await client.PostAsync("https://accounts.spotify.com/api/token", content);
            var body = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(body);

            _tokens = new SpotifyToken
            {
                AccessToken = json["access_token"]?.ToString(),
                RefreshToken = json["refresh_token"]?.ToString(),
                ExpiryTime = DateTime.UtcNow.AddSeconds((int)json["expires_in"])
            };

            SaveTokens();
            OutputBox.Text = Translations.LOGIN_SUCCESS;
        }

        private async Task RefreshAccessToken()
        {
            if (_tokens == null || string.IsNullOrEmpty(_tokens.RefreshToken)) return;

            using var client = _httpClientFactory.CreateClient();
            var postData = new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", _tokens.RefreshToken),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret)
            };

            var content = new FormUrlEncodedContent(postData);
            var response = await client.PostAsync("https://accounts.spotify.com/api/token", content);
            var body = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(body);

            _tokens.AccessToken = json["access_token"]?.ToString();
            _tokens.ExpiryTime = DateTime.UtcNow.AddSeconds((int)json["expires_in"]);
            SaveTokens();
        }

        private void SaveTokens()
        {
            _tokens.ClientId = _clientId;
            _tokens.ClientSecret = _clientSecret;
            Directory.CreateDirectory(Path.GetDirectoryName(_tokenPath));
            File.WriteAllText(_tokenPath, JsonConvert.SerializeObject(_tokens));
        }

        private async Task LoadTokens()
        {
            if (!File.Exists(_tokenPath)) return;

            try
            {
                _tokens = JsonConvert.DeserializeObject<SpotifyToken>(await File.ReadAllTextAsync(_tokenPath));
                if (!string.IsNullOrEmpty(_tokens.ClientId) && !string.IsNullOrEmpty(_tokens.ClientSecret))
                {
                    _clientId = _tokens.ClientId;
                    _clientSecret = _tokens.ClientSecret;
                }

                if (_tokens.ExpiryTime <= DateTime.UtcNow)
                {
                    _ = RefreshAccessToken(); // fire-and-forget auto-refresh
                }

                await UpdateTrackInfo();
            }
            catch
            {
                _tokens = null;
            }
        }

        private async Task UpdateTrackInfo()
        {
            if (_tokens == null || string.IsNullOrEmpty(_tokens.AccessToken))
            {
                output = Translations.LOGIN_FAILED;
                OutputBox.Text = output;
                File.WriteAllText(_outputFilePath, output);
                return;
            }

            // Refresh token if expired
            if (_tokens.ExpiryTime <= DateTime.UtcNow)
            {
                await RefreshAccessToken();
                if (_tokens == null || string.IsNullOrEmpty(_tokens.AccessToken))
                {
                    OutputBox.Text = output;
                    File.WriteAllText(_outputFilePath, output);
                    return;
                }
            }

            output = "";
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _tokens.AccessToken);
                var response = await client.GetAsync("https://api.spotify.com/v1/me/player/currently-playing");
                if (!response.IsSuccessStatusCode)
                {
                    _tokens = new SpotifyToken();
                    Directory.CreateDirectory(Path.GetDirectoryName(_tokenPath));
                    File.WriteAllText(_tokenPath, JsonConvert.SerializeObject(_tokens));
                    return;
                }

                var content = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(content))
                {
                    output = Translations.NOTHING_PLAYING;
                    OutputBox.Text = output;
                    File.WriteAllText(_outputFilePath, output);
                }
                else
                {
                    var json = JsonConvert.DeserializeObject<SpotifyResponse>(content);

                    if (json.Item == null) return;

                    string trackId = json.Item.Id;
                    _isPlaying = Convert.ToBoolean(json.IsPlaying);
                    string artist = json.Item.Artists[0].Name ?? Translations.UNKNOWN_ARTIST;
                    string title = json.Item.Name ?? Translations.UNKNOWN_TITLE;
                    int progressMs = Convert.ToInt32(json.ProgressMs ?? 0);
                    int durationMs = Convert.ToInt32(json.Item.DurationMs ?? 1);

                    _currentTrackId = trackId;
                    _currentSongDuration = durationMs;
                    _initialProgressMs = progressMs;
                    _songStartTime = DateTime.UtcNow - TimeSpan.FromMilliseconds(_initialProgressMs);

                    output = "";
                    if (ArtistCheckbox.IsChecked == true)
                        output += artist;

                    if (TitleCheckbox.IsChecked == true)
                    {
                        if (!string.IsNullOrEmpty(output)) output += " - ";
                        output += title;
                    }

                    if (TimeCheckbox.IsChecked == true)
                    {
                        if (!string.IsNullOrEmpty(output)) output += " ";
                        output += $"{FormatTime(progressMs)} / {FormatTime(durationMs)}";
                    }

                    OutputBox.Text = output;
                    File.WriteAllText(_outputFilePath, output);
                    _lastArtist = artist;
                    _lastTitle = title;
                    SaveTokens();
                }
            }
            catch (Exception ex)
            {
                OutputBox.Text = $"Error: {ex.Message}";
            }
        }

        private void UpdateDisplayedText()
        {
            if (string.IsNullOrEmpty(_currentTrackId))
            {
                OutputBox.Text = output;
                File.WriteAllText(_outputFilePath, output);
                return;
            }

            output = "";
            string artist = _lastArtist ?? "Unknown Artist";
            string title = _lastTitle ?? "Unknown Title";
            int elapsed;
            if (_isPlaying)
                elapsed = (int)(DateTime.UtcNow - _songStartTime).TotalMilliseconds;
            else
            {
                elapsed = _initialProgressMs;
            }

            if (elapsed >= _currentSongDuration)
            {
                _currentTrackId = null;
                _ = UpdateTrackInfo();
                return;
            }


            if (ArtistCheckbox.IsChecked == true)
                output += artist;

            if (TitleCheckbox.IsChecked == true)
            {
                if (!string.IsNullOrEmpty(output)) output += " - ";
                output += title;
            }

            if (TimeCheckbox.IsChecked == true)
            {
                if (!string.IsNullOrEmpty(output)) output += " ";
                output += $"{FormatTime(elapsed)} / {FormatTime(_currentSongDuration)}";
            }

            OutputBox.Text = output;
            File.WriteAllText(_outputFilePath, output);
        }

        private void ShowAboutWindow_Click(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow
            {
                Owner = this
            };
            about.ShowDialog();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow helpWindow = new HelpWindow();
            helpWindow.Owner = this;
            helpWindow.ShowDialog();
        }

        private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageSelector.SelectedItem is ComboBoxItem selectedItem)
            {
                var cultureCode = selectedItem.Tag.ToString();
                if (!string.IsNullOrEmpty(cultureCode))
                {
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo(cultureCode);
                    Thread.CurrentThread.CurrentCulture = new CultureInfo(cultureCode);
                    Settings.LangCode = cultureCode;
                    // Optionally, save to settings
                    Settings.SaveSettings();
                    NameLabels();
                }
            }
        }

        private void UseCustomCredentialsCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            CustomCredentialsPanel.Visibility = Visibility.Visible;
        }

        private void UseCustomCredentialsCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            CustomCredentialsPanel.Visibility = Visibility.Collapsed;
        }
    }
}
// This software is licensed under CC BY-NC-ND 4.0.
// Free for personal, non-commercial use only.
// Do not modify, distribute, or sell without written permission.
// (c) 2025 Tom "TKoNoR"