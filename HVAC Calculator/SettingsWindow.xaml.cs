using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace HVACCalculator;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            WindowStateManager.RegisterWindow(this);
            if (Owner != null)
            {
                Left = Owner.Left + (Owner.ActualWidth - ActualWidth) / 2;
                Top = Owner.Top + (Owner.ActualHeight - ActualHeight) / 2;
            }

            tbMinVelocityCvgkw.Text = AppSettings.MinVelocityCvgkw.ToString("0.##", CultureInfo.CurrentCulture);
            tbMaxVelocityCvgkw.Text = AppSettings.MaxVelocityCvgkw.ToString("0.##", CultureInfo.CurrentCulture);
            tbMinVelocityTapwater.Text = AppSettings.MinVelocityTapwater.ToString("0.##", CultureInfo.CurrentCulture);
            tbMaxVelocityTapwater.Text = AppSettings.MaxVelocityTapwater.ToString("0.##", CultureInfo.CurrentCulture);
        };
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb) tb.SelectAll();
    }

    private void btnOpslaan_Click(object sender, RoutedEventArgs e)
    {
        bool cvMinOk = double.TryParse(tbMinVelocityCvgkw.Text, NumberStyles.Float | NumberStyles.AllowThousands,
            CultureInfo.CurrentCulture, out double cvMin) && cvMin > 0;
        bool cvMaxOk = double.TryParse(tbMaxVelocityCvgkw.Text, NumberStyles.Float | NumberStyles.AllowThousands,
            CultureInfo.CurrentCulture, out double cvMax) && cvMax > 0;

        bool tapMinOk = double.TryParse(tbMinVelocityTapwater.Text, NumberStyles.Float | NumberStyles.AllowThousands,
            CultureInfo.CurrentCulture, out double tapMin) && tapMin > 0;
        bool tapMaxOk = double.TryParse(tbMaxVelocityTapwater.Text, NumberStyles.Float | NumberStyles.AllowThousands,
            CultureInfo.CurrentCulture, out double tapMax) && tapMax > 0;

        if (!cvMinOk || !cvMaxOk || !tapMinOk || !tapMaxOk)
        {
            MessageBox.Show("Voer geldige positieve waarden in voor alle snelheden.", "Fout",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (cvMin >= cvMax)
        {
            MessageBox.Show("Voor CV/GKW moet de minimale snelheid kleiner zijn dan de maximale snelheid.", "Fout",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (tapMin >= tapMax)
        {
            MessageBox.Show("Voor Tapwater moet de minimale snelheid kleiner zijn dan de maximale snelheid.", "Fout",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        AppSettings.MinVelocityCvgkw = cvMin;
        AppSettings.MaxVelocityCvgkw = cvMax;
        AppSettings.MinVelocityTapwater = tapMin;
        AppSettings.MaxVelocityTapwater = tapMax;
        AppSettings.Save();

        Close();
    }

    private void btnSluiten_Click(object sender, RoutedEventArgs e) => Close();
}
