using System.Windows;
using System.Windows.Controls;
using CurrentlyListening.Properties;
using CurrentlyListening.Resources;

namespace CurrentlyListening.Windows
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            
            // Load current setting into UI
            AppSettings.LoadSettings();
            NameLabels();
            CloseShouldMinimize.IsChecked = Properties.AppSettings.CloseToTray;
            StartMinimized.IsChecked = AppSettings.StartMinimized;
            CheckForUpdates.IsChecked = AppSettings.CheckForUpdates;
            TrailingSpacesSlider.Value = Properties.AppSettings.TrailingSpacesCount;
            TrailingSpacesTextBox.Text = Properties.AppSettings.TrailingSpacesCount.ToString();
        }

        private void NameLabels()
        {
            CloseShouldMinimize.Content = Translations.CLOSE_SHOULD_MINIMIZE;
            StartMinimized.Content = Translations.START_MINIMIZED;
            TrailingSpaces.Text = Translations.TRAILING_SPACES_TEXT;
            CheckForUpdates.Content = Translations.CHECK_FOR_UPDATES;
        }

        private void CloseShouldMinimize_OnClick(object sender, RoutedEventArgs e)
        {
            Properties.AppSettings.CloseToTray = CloseShouldMinimize.IsChecked.HasValue && CloseShouldMinimize.IsChecked.Value;
        }

        private void StartMinimized_OnClick(object sender, RoutedEventArgs e)
        {
            Properties.AppSettings.StartMinimized = StartMinimized.IsChecked.HasValue && StartMinimized.IsChecked.Value;
        }
        private bool _isUpdatingTrailingSpaces;
        private void TrailingSpacesSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingTrailingSpaces)
                return;

            _isUpdatingTrailingSpaces = true;
            TrailingSpacesTextBox.Text = ((int)TrailingSpacesSlider.Value).ToString();
            _isUpdatingTrailingSpaces = false;
            AppSettings.TrailingSpacesCount = (int)TrailingSpacesSlider.Value;
        }

        private void TrailingSpacesTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingTrailingSpaces)
                return;

            if (int.TryParse(TrailingSpacesTextBox.Text, out int value))
            {
                if (value < 0)
                    value = 0;
                if (value > 50)
                    value = 50;

                _isUpdatingTrailingSpaces = true;
                TrailingSpacesSlider.Value = value;
                _isUpdatingTrailingSpaces = false;
                AppSettings.TrailingSpacesCount = (int)TrailingSpacesSlider.Value;
            }
        }

        private void CheckForUpdates_OnClick(object sender, RoutedEventArgs e)
        {
            Properties.AppSettings.CheckForUpdates = CheckForUpdates.IsChecked.HasValue && CheckForUpdates.IsChecked.Value;
        }
    }
}