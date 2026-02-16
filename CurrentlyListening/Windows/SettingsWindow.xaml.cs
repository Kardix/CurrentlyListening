using System.Windows;
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
        }

        private void NameLabels()
        {
            CloseShouldMinimize.Content = Translations.CLOSE_SHOULD_MINIMIZE;
            StartMinimized.Content = Translations.START_MINIMIZED;
        }

        private void CloseShouldMinimize_OnClick(object sender, RoutedEventArgs e)
        {
            Properties.AppSettings.CloseToTray = CloseShouldMinimize.IsChecked.HasValue && CloseShouldMinimize.IsChecked.Value;
        }

        private void StartMinimized_OnClick(object sender, RoutedEventArgs e)
        {
            Properties.AppSettings.StartMinimized = StartMinimized.IsChecked.HasValue && StartMinimized.IsChecked.Value;
        }
    }
}