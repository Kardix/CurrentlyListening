using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using CurrentlyListening.Resources;

namespace CurrentlyListening.Windows;

public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();
        HowToUse.Text = Translations.HOW_TO_USE_APP;
        Help1.Inlines.Add(new Run(Translations.HELP_1_0 + " "));
        Help1.Inlines.Add(new Hyperlink( new Run("https://developer.spotify.com/dashboard")){NavigateUri = new Uri("https://developer.spotify.com/dashboard")});
        Help1.Inlines.Add(new Run(" "+ Translations.HELP_1_1));
        Help2.Inlines.Add(new Run(" "+ Translations.HELP_2));
        Help3.Inlines.Add(new Run(" "+ Translations.HELP_3));
        Help4.Inlines.Add(new Run(" "+ Translations.HELP_4));
        Help5.Inlines.Add(new Run(" "+ Translations.HELP_5));
        HelpTip.Inlines.Add(new Run(" "+ Translations.HELP_TIP));
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
// This software is licensed under CC BY-NC-ND 4.0.
// Free for personal, non-commercial use only.
// Do not modify, distribute, or sell without written permission.
// (c) 2025 Tom "TKoNoR"