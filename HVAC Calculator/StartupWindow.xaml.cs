using System.Windows;

namespace HVACCalculator;

public partial class StartupWindow : Window
{
    public StartupWindow()
    {
        InitializeComponent();
        Loaded += (s, e) => WindowStateManager.RegisterWindow(this);
    }

    private void btnStartAirDuct_Click(object sender, RoutedEventArgs e)
    {
        var win = new AirDuctWindow();
        win.Owner = this;
        win.Show();
    }

    private void btnStartCVGKW_Click(object sender, RoutedEventArgs e)
    {
        var win = new CVGKWWindow();
        win.Owner = this;
        win.Show();
    }

    private void btnStartTapwater_Click(object sender, RoutedEventArgs e)
    {
        var win = new TapwaterWindow();
        win.Owner = this;
        win.Show();
    }
}