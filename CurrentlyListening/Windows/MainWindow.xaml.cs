using System.ComponentModel;
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
using System.Windows.Input;
using System.Windows.Threading;
using CurrentlyListening.Models;
using CurrentlyListening.Properties;
using CurrentlyListening.Resources;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

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
        private DateTime _songStartTime;
        private bool _loginPopupShown; // the "log in?" popup is shown once until the next successful login
        private HttpListener _loginListener; // the login callback listener while a login is in progress
        private static readonly TimeSpan LoginTimeout = TimeSpan.FromMinutes(5);
        private Task _refreshTask; // the token refresh in progress, shared by everyone who needs one
        private DispatcherTimer _spotifyPollTimer = new();


        private SpotifyToken _tokens;
        string output = "";
        private TrayIcon _trayIcon;
        private MenuItem _exitItem;

        // Settings are already loaded by App.OnStartup before this window is created.
        public MainWindow(IHttpClientFactory httpClientFactory)
        {
            InitializeComponent();
            VersionText.Text = $"v{AppVersion.Display}";
            var culture = AppSettings.GetCulture();
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            ArtistCheckbox.IsChecked = AppSettings.ShowArtist;
            TitleCheckbox.IsChecked = AppSettings.ShowTitle;
            DurationCheckbox.IsChecked = AppSettings.ShowDuration;
            LanguageSelector.SelectionChanged -= LanguageSelector_SelectionChanged;

            foreach (var item in LanguageSelector.Items)
            {
                if (item is ComboBoxItem comboItem && comboItem.Tag?.ToString() == AppSettings.LangCode)
                {
                    LanguageSelector.SelectedItem = comboItem;
                    break;
                }
            }

            LanguageSelector.SelectionChanged += LanguageSelector_SelectionChanged;
            InitializeTray();
            NameLabels();

            if (AppSettings.EnsureOutputFileIsWritable())
            {
                MessageBox.Show(string.Format(Translations.OUTPUT_FILE_MOVED, AppSettings.OutputFilePath),
                    Translations.APP_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            _httpClientFactory = httpClientFactory;
            _ = LoadTokens();

            _displayTimer.Interval = TimeSpan.FromSeconds(1);
            _displayTimer.Tick += (s, e) => UpdateDisplayedText();
            _displayTimer.Start();

            _spotifyPollTimer.Interval = TimeSpan.FromSeconds(5);
            _spotifyPollTimer.Tick += async (s, e) => await UpdateTrackInfo();
            _spotifyPollTimer.Start();

            Closing += MainWindow_Closing;

            if (AppSettings.CheckForUpdates)
                _ = CheckForUpdatesAsync();
        }

        private void InitializeTray()
        {
            _exitItem = new MenuItem { Header = Translations.CLOSE };
            _exitItem.Click += ExitItem_Click;

            var trayMenu = new ContextMenu();
            trayMenu.Items.Add(_exitItem);

            _trayIcon = new TrayIcon(new Uri("pack://application:,,,/Resources/icon.ico"))
            {
                ContextMenu = trayMenu
            };

            // Every exit that runs code (tray Close, window close, update, Windows signing out)
            // ends in Application.Exit; removing the icon there leaves no dead icon behind.
            Application.Current.Exit += (s, e) => _trayIcon.Dispose();

            _trayIcon.DoubleClick += (s, e) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!IsVisible)
                        Show();

                    if (WindowState == WindowState.Minimized)
                        WindowState = WindowState.Normal;

                    ShowInTaskbar = true;
                    Activate();
                }));
            };
        }

        private void ExitItem_Click(object sender, RoutedEventArgs e)
        {
            _trayIcon.Dispose();
            Closing -= MainWindow_Closing;
            Application.Current.Shutdown();
        }

        private void NameLabels()
        {
            _trayIcon.Text = $"{Translations.APP_NAME} {AppVersion.Display}";
            TrackDisplay.Text = Translations.TRACK_DISPLAY_OPTIONS;
            ArtistCheckbox.Content = Translations.ARTIST;
            TitleCheckbox.Content = Translations.SONG_TITLE;
            DurationCheckbox.Content = Translations.TIME;
            HelpButton.Content = Translations.HELP;
            OutputPreview.Text = Translations.OUTPUT_PREVIEW;
            UpdateDisplay.Content = Translations.UPDATE_DISPLAY;
            LoginToSpotify.Content = Translations.LOGIN_SPOTIFY;
            OutputFileLocation.Content = Translations.SET_OUTPUT_LOCATION;
            UseCustomCredentialsCheckbox.Content = Translations.USE_CUSTOM_CREDENTIALS;
            ClientIdText.Text = Translations.CLIENT_ID;
            ClientSecretText.Text = Translations.CLIENT_SECRET;
            CreatedBy.Text = Translations.CREATED_BY;
            _exitItem.Header = Translations.CLOSE;
            Settings.Content = Translations.SETTINGS;
        }

        private void SetOutputFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = Translations.SELECT_OUTPUT,
                // Open where the file is now, so users can see where track info is written.
                InitialDirectory = Path.GetDirectoryName(AppSettings.OutputFilePath),
                FileName = Path.GetFileName(AppSettings.OutputFilePath)
            };

            if (dialog.ShowDialog() == true)
            {
                if (!AppSettings.CanWriteOutputFile(dialog.FileName))
                {
                    MessageBox.Show(string.Format(Translations.OUTPUT_FILE_NOT_WRITABLE, dialog.FileName),
                        Translations.APP_NAME, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // The old file would otherwise keep the last song forever for any overlay still reading it.
                var oldPath = AppSettings.OutputFilePath;
                if (!string.Equals(Path.GetFullPath(oldPath), Path.GetFullPath(dialog.FileName),
                        StringComparison.OrdinalIgnoreCase))
                    AppSettings.TryWriteFile(oldPath, Translations.OUTPUT_FILE_RELINK);

                AppSettings.OutputFilePath = dialog.FileName; // the setter saves

                MessageBox.Show(string.Format(Translations.FILE_SAVED, dialog.FileName), Translations.FILE_SAVED_CAPTION,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Fetches the current track right away instead of waiting for the next 5-second poll.
        private async void UpdateDisplay_Click(object sender, RoutedEventArgs e)
        {
            await UpdateTrackInfo();
        }

        // Shows the text in the preview and writes it to the output file. A failed write
        // (file locked, folder gone) is shown in the preview instead of being thrown on
        // every timer tick and filling the crash log.
        private void WriteOutput(string text)
        {
            OutputBox.Text = text;
            try
            {
                AppSettings.WriteOutputFile(text);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                OutputBox.Text = $"Error: {ex.Message}";
            }
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

            // An earlier attempt may still be waiting (e.g. its browser tab was closed);
            // stop it so this one can take the port.
            StopLoginListener();

            var listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:5000/callback/");
            try
            {
                listener.Start();
            }
            catch (HttpListenerException)
            {
                listener.Close();
                MessageBox.Show(Translations.LOGIN_PORT_IN_USE, Translations.APP_NAME, MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            _loginListener = listener;
            try
            {
                Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
            {
                // No default browser (or it failed to start): free the port and say why.
                StopLoginListener();
                MessageBox.Show(string.Format(Translations.LOGIN_ERROR, ex.Message), Translations.APP_NAME,
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            HttpListenerContext context;
            // Give up if the browser never comes back (tab closed), so the port is freed.
            using (var timeout = new CancellationTokenSource(LoginTimeout))
            using (timeout.Token.Register(() => listener.Close()))
            {
                try
                {
                    context = await listener.GetContextAsync();
                }
                catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException)
                {
                    // Timed out, or replaced by a newer login attempt.
                    if (_loginListener == listener)
                        StopLoginListener();
                    return;
                }
            }

            string error = null;
            try
            {
                var query = HttpUtility.ParseQueryString(context.Request.Url.Query);
                var code = query.Get("code"); // missing when the user pressed Cancel (error=access_denied)

                string page;
                if (string.IsNullOrEmpty(code))
                {
                    page = Translations.LOGIN_CANCELLED;
                }
                else
                {
                    // Exchange before answering the browser, so the page shows the real result.
                    try
                    {
                        error = await ExchangeCodeForTokens(code);
                    }
                    catch (Exception ex)
                    {
                        // Network failure or timeout, an unreadable reply, or tokens.json not writable.
                        error = ex.Message;
                    }
                    page = error == null ? Translations.AUTH_SUCCESS : string.Format(Translations.LOGIN_ERROR, error);
                }

                try
                {
                    var buffer = Encoding.UTF8.GetBytes(
                        $"<html><body>{WebUtility.HtmlEncode(page).Replace("\n", "<br>")}</body></html>");
                    context.Response.ContentType = "text/html; charset=utf-8";
                    context.Response.ContentLength64 = buffer.Length;
                    await context.Response.OutputStream.WriteAsync(buffer);
                    context.Response.OutputStream.Close();
                }
                catch (Exception ex) when (ex is HttpListenerException or IOException)
                {
                    // The browser went away before the page was sent; the login result still stands.
                }
            }
            finally
            {
                if (_loginListener == listener)
                    StopLoginListener();
            }

            if (error != null)
            {
                MessageBox.Show(string.Format(Translations.LOGIN_ERROR, error), Translations.APP_NAME,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopLoginListener()
        {
            _loginListener?.Close();
            _loginListener = null;
        }

        // Returns null on success, otherwise a message describing why Spotify refused the login.
        // Network and file errors are thrown; LoginButton_Click reports them the same way.
        private async Task<string> ExchangeCodeForTokens(string code)
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

            if (!response.IsSuccessStatusCode)
            {
                // e.g. {"error":"invalid_client","error_description":"Invalid client secret"}
                try
                {
                    var errorJson = JObject.Parse(body);
                    return errorJson["error_description"]?.ToString() ?? errorJson["error"]?.ToString()
                        ?? response.StatusCode.ToString();
                }
                catch (JsonException)
                {
                    return response.StatusCode.ToString();
                }
            }

            var json = JObject.Parse(body);

            _tokens = new SpotifyToken
            {
                AccessToken = json["access_token"]?.ToString(),
                RefreshToken = json["refresh_token"]?.ToString(),
                ExpiryTime = DateTime.UtcNow.AddSeconds((int)json["expires_in"])
            };

            SaveTokens();
            _loginPopupShown = false;
            OutputBox.Text = Translations.LOGIN_SUCCESS;
            return null;
        }

        // Startup, the poll tick, the end-of-song check and "Update display" can all need a
        // refresh at once. They share the one in progress: a second refresh with the same
        // refresh token can be rejected once Spotify has rotated it, which would log the user out.
        private Task RefreshAccessToken()
        {
            if (_refreshTask == null || _refreshTask.IsCompleted)
                _refreshTask = RefreshAccessTokenCore();
            return _refreshTask;
        }

        private async Task RefreshAccessTokenCore()
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

            if (!response.IsSuccessStatusCode)
            {
                // 400 means the refresh token was revoked or the credentials are wrong, so the user
                // has to log in again. Anything else (outage, rate limit) is transient: keep the
                // tokens and let the next poll retry.
                if (response.StatusCode == HttpStatusCode.BadRequest)
                    _tokens.AccessToken = null;
                return;
            }

            var body = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(body);

            _tokens.AccessToken = json["access_token"]?.ToString();
            _tokens.ExpiryTime = DateTime.UtcNow.AddSeconds((int)json["expires_in"]);

            // Spotify may rotate the refresh token; the old one stops working once it does.
            var newRefreshToken = json["refresh_token"]?.ToString();
            if (!string.IsNullOrEmpty(newRefreshToken))
                _tokens.RefreshToken = newRefreshToken;

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
            }
            catch
            {
                // Only an unreadable token file means "logged out".
                _tokens = null;
                return;
            }

            if (_tokens == null) return;

            if (!string.IsNullOrEmpty(_tokens.ClientId) && !string.IsNullOrEmpty(_tokens.ClientSecret))
            {
                _clientId = _tokens.ClientId;
                _clientSecret = _tokens.ClientSecret;
            }

            // UpdateTrackInfo refreshes an expired token itself and keeps the tokens on
            // network failures, so a slow network at startup no longer forces a re-login.
            await UpdateTrackInfo();
        }

        private async Task UpdateTrackInfo()
        {
            if (_tokens == null || string.IsNullOrEmpty(_tokens.AccessToken))
            {
                output = Translations.LOGIN_FAILED;
                WriteOutput(output);
                // MessageBox.Show keeps pumping messages, so the poll timer keeps ticking while it's
                // open; set the flag first so later ticks don't stack more popups on top of it.
                if (AppSettings.ShowPopupOnFail && !_loginPopupShown)
                {
                    _loginPopupShown = true;
                    var result = MessageBox.Show(
                        $"{Translations.LOGIN_POPUP_TEXT}",
                        Translations.LOGIN_SPOTIFY,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                        LoginButton_Click(null, null);
                }

                return;
            }

            try
            {
                // Refresh token if expired. Inside the try so a network failure is shown
                // and retried on the next poll instead of escaping the timer tick.
                if (_tokens.ExpiryTime <= DateTime.UtcNow)
                {
                    await RefreshAccessToken();
                    if (_tokens == null || string.IsNullOrEmpty(_tokens.AccessToken) ||
                        _tokens.ExpiryTime <= DateTime.UtcNow)
                    {
                        WriteOutput(output);
                        return;
                    }
                }

                output = "";
                using var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _tokens.AccessToken);
                
                var response = await client.GetAsync("https://api.spotify.com/v1/me/player/currently-playing");
                if (!response.IsSuccessStatusCode)
                {
                    if(response.StatusCode == HttpStatusCode.NoContent)
                        return;
                    await RefreshAccessToken();
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _tokens.AccessToken);
                    response = await client.GetAsync("https://api.spotify.com/v1/me/player/currently-playing");
                    if (!response.IsSuccessStatusCode)
                    {
                        return;
                    }
                }

                var content = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(content))
                {
                    output = Translations.NOTHING_PLAYING;
                    _isPlaying = false;
                    WriteOutput(output);
                }
                else
                {
                    var json = JsonConvert.DeserializeObject<SpotifyResponse>(content);

                    if (json.Item == null) return;

                    string trackId = json.Item.Id;
                    _isPlaying = Convert.ToBoolean(json.IsPlaying);
                    if (!_isPlaying)
                    {
                        output = Translations.NOTHING_PLAYING;
                        WriteOutput(output);
                        return;
                    }

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

                    if (DurationCheckbox.IsChecked == true)
                    {
                        if (!string.IsNullOrEmpty(output)) output += " ";
                        output += $"{FormatTime(progressMs)} / {FormatTime(durationMs)}";
                    }

                    WriteOutput(output);
                    _lastArtist = artist;
                    _lastTitle = title;
                }
            }
            catch (Exception ex)
            {
                OutputBox.Text = $"Error: {ex.Message}";
            }
        }

        private void UpdateDisplayedText()
        {
            if (!_isPlaying)
            {
                output = Translations.NOTHING_PLAYING;
                WriteOutput(output);
                return;
            }

            if (string.IsNullOrEmpty(_currentTrackId))
            {
                WriteOutput(output);
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

            if (DurationCheckbox.IsChecked == true)
            {
                if (!string.IsNullOrEmpty(output)) output += " ";
                output += $"{FormatTime(elapsed)} / {FormatTime(_currentSongDuration)}";
            }

            WriteOutput(output);
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

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
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
                    AppSettings.LangCode = cultureCode;
                    // Optionally, save to settings
                    AppSettings.SaveSettings();
                    NameLabels();
                }
            }
        }

        private void UseCustomCredentialsCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            CustomCredentialsPanel.Visibility = Visibility.Visible;
            Window.Height += 80;
            ClientIdTextBox.Text = _clientId;
            ClientSecretTextBox.Text = _clientSecret;
        }

        private void UseCustomCredentialsCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            CustomCredentialsPanel.Visibility = Visibility.Collapsed;
            Window.Height -= 80;
        }

        private void DurationCheckbox_OnClick(object sender, RoutedEventArgs e)
        {
            AppSettings.ShowDuration = DurationCheckbox.IsChecked.HasValue && DurationCheckbox.IsChecked.Value;
        }

        private void TitleCheckbox_OnClick(object sender, RoutedEventArgs e)
        {
            AppSettings.ShowTitle = TitleCheckbox.IsChecked.HasValue && TitleCheckbox.IsChecked.Value;
        }

        private void ArtistCheckbox_OnClick(object sender, RoutedEventArgs e)
        {
            AppSettings.ShowArtist = ArtistCheckbox.IsChecked.HasValue && ArtistCheckbox.IsChecked.Value;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (AppSettings.CloseToTray)
            {
                // Cancel normal close
                e.Cancel = true;

                // Hide instead
                Hide();

                // Tell the user the app is still running in the tray.
                _trayIcon?.ShowBalloon(Translations.APP_NAME, Translations.MINIMIZE_NOTIFICATION_TEXT);
            }
        }

        private static Version ParseVersion(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return new Version(0, 0, 0, 0);

            tag = tag.Trim();

            if (tag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                tag = tag.Substring(1);

            return Version.TryParse(tag, out var version)
                ? version
                : new Version(0, 0, 0, 0);
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();

                client.DefaultRequestHeaders.UserAgent.Clear();
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("CurrentlyListening", "1.0"));

                var response =
                    await client.GetAsync("https://api.github.com/repos/Kardix/CurrentlyListening/releases/latest");

                if (!response.IsSuccessStatusCode)
                    return;

                var json = await response.Content.ReadAsStringAsync();
                var latestRelease = JsonConvert.DeserializeObject<GitHubLatestRelease>(json);

                if (latestRelease == null || string.IsNullOrWhiteSpace(latestRelease.TagName))
                    return;

                var currentVersion = AppVersion.Current;
                var latestVersion = ParseVersion(latestRelease.TagName);

                if (latestVersion > currentVersion)
                {
                    var result = MessageBox.Show(
                        $"{Translations.NEW_VERSION_AVAILABLE}\n\n{Translations.CURRENT_VERSION} {currentVersion}\n{Translations.LATEST_VERSION} {latestVersion}\n\n{Translations.INSTALL_UPDATE}",
                        Translations.UPDATE_AVAILABLE,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes &&
                        !string.IsNullOrWhiteSpace(latestRelease.Assets.FirstOrDefault()?.BrowserDownloadUrl
                            .ToString()))
                    {
                        await DownloadAndInstallUpdateAsync(latestRelease.Assets.FirstOrDefault()?.BrowserDownloadUrl);
                    }
                }
            }
            catch
            {
                // optional: log silently, but don't annoy the user on startup
            }
        }

        private async Task DownloadAndInstallUpdateAsync(Uri installerUrl)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();

                client.DefaultRequestHeaders.UserAgent.ParseAdd("CurrentlyListening");

                var tempInstallerPath = Path.Combine(
                    Path.GetTempPath(),
                    "CurrentlyListening.Update.exe");

                await using (var stream = await client.GetStreamAsync(installerUrl))
                await using (var fileStream = new FileStream(
                                 tempInstallerPath,
                                 FileMode.Create,
                                 FileAccess.Write,
                                 FileShare.None))
                {
                    await stream.CopyToAsync(fileStream);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = tempInstallerPath,
                    Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
                    UseShellExecute = true
                });

                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Update failed: {ex.Message}",
                    "Update failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
                return;
            }

            DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
    }
}
// This software is licensed under CC BY-NC-ND 4.0.
// Free for personal, non-commercial use only.
// Do not modify, distribute, or sell without written permission.
// (c) 2026 Tom "TKoNoR"