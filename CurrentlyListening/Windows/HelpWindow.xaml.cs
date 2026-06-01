using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using CurrentlyListening.Resources;
using Cursors = System.Windows.Input.Cursors;

namespace CurrentlyListening.Windows;

public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();
        HowToUse.Text = Translations.HOW_TO_USE_APP;
        Help1.AppendText(Translations.HELP_1_0 + " ");
        var hyperlink = new Hyperlink(new Run("https://developer.spotify.com/dashboard"))
        {
            NavigateUri = new Uri("https://developer.spotify.com/dashboard"),
            Foreground = Brushes.DodgerBlue,
            Cursor = Cursors.Hand
        };

        hyperlink.RequestNavigate += (sender, e) =>
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });

            e.Handled = true;
        };
        ClickableURL.Inlines.Add(hyperlink);
        Help1.AppendText(" " + Translations.HELP_1_1);
        Help2.Text += (" " + Translations.HELP_2);
        Help3.Text += (" " + Translations.HELP_3);
        Help4.Text += (" " + Translations.HELP_4);
        Help5.Text += (" " + Translations.HELP_5);
        HelpTip.Inlines.Add(new Run(" " + Translations.HELP_TIP));
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
// (c) 2026 Tom "TKoNoR"