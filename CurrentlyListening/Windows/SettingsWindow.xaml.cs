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
            CloseShouldMinimize.IsChecked = AppSettings.CloseToTray;
            StartMinimized.IsChecked = AppSettings.StartMinimized;
            CheckForUpdates.IsChecked = AppSettings.CheckForUpdates;
            TrailingSpacesSlider.Value = AppSettings.TrailingSpacesCount;
            TrailingSpacesTextBox.Text = AppSettings.TrailingSpacesCount.ToString();
            ShowPopUpOnFail.IsChecked = AppSettings.ShowPopupOnFail;
            
        }

        private void NameLabels()
        {
            WindowSettings.Title = Translations.SETTINGS;
            SettingsLabel.Text = Translations.SETTINGS_GENERAL;
            CloseShouldMinimize.Content = Translations.CLOSE_SHOULD_MINIMIZE;
            StartMinimized.Content = Translations.START_MINIMIZED;
            TrailingSpaces.Text = Translations.TRAILING_SPACES_TEXT;
            CheckForUpdates.Content = Translations.CHECK_FOR_UPDATES;
            ShowPopUpOnFail.Content = Translations.AUTH_FAIL_POPUP;
            
        }

        private void CloseShouldMinimize_OnClick(object sender, RoutedEventArgs e)
        {
            AppSettings.CloseToTray = CloseShouldMinimize.IsChecked.HasValue && CloseShouldMinimize.IsChecked.Value;
        }

        private void StartMinimized_OnClick(object sender, RoutedEventArgs e)
        {
            AppSettings.StartMinimized = StartMinimized.IsChecked.HasValue && StartMinimized.IsChecked.Value;
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
            AppSettings.CheckForUpdates = CheckForUpdates.IsChecked.HasValue && CheckForUpdates.IsChecked.Value;
        }

        private void ShowPopUpOnFail_OnClick(object sender, RoutedEventArgs e)
        {
            AppSettings.ShowPopupOnFail =
                ShowPopUpOnFail.IsChecked.HasValue && ShowPopUpOnFail.IsChecked.Value;
        }
    }
}
// This software is licensed under CC BY-NC-ND 4.0.
// Free for personal, non-commercial use only.
// Do not modify, distribute, or sell without written permission.
// (c) 2026 Tom "TKoNoR"