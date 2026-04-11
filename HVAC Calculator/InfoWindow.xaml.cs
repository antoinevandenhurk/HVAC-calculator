using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace HVACCalculator;

public partial class InfoWindow : Window
{
    public InfoWindow()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            if (Owner != null)
            {
                Left = Owner.Left + (Owner.ActualWidth - ActualWidth) / 2;
                Top = Owner.Top + Owner.ActualHeight - ActualHeight;
            }
        };
    }

    private void btnKofi_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://ko-fi.com/antoinevandenhurk",
            UseShellExecute = true
        });
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true
        });
        e.Handled = true;
    }
}