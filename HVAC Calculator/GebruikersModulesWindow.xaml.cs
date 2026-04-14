using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace HVACCalculator;

public partial class GebruikersModulesWindow : Window
{
    private enum ModuleType
    {
        Module5,
        Module6,
        Module7
    }

    public GebruikersModulesWindow()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            WindowStateManager.RegisterWindow(this);
            if (Owner != null)
            {
                Left = Owner.Left + (Owner.ActualWidth - ActualWidth) / 2;
                Top = Owner.Top + Owner.ActualHeight - ActualHeight;
            }
            ApplyModuleLayout();
        };
    }

    private ModuleType SelectedModule => cbGebruikersModule.SelectedIndex switch
    {
        1 => ModuleType.Module6,
        2 => ModuleType.Module7,
        _ => ModuleType.Module5
    };

    private static bool TryGetDouble(string text, out double value)
    {
        return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value);
    }

    private static string FormatDouble(double value)
    {
        return value.ToString("0.###", CultureInfo.CurrentCulture);
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private void ApplyModuleLayout()
    {
        bool hasBypass = SelectedModule != ModuleType.Module6;

        panelQv2.Visibility = hasBypass ? Visibility.Visible : Visibility.Collapsed;
        panelQv5.Visibility = hasBypass ? Visibility.Visible : Visibility.Collapsed;

        string imageName = SelectedModule switch
        {
            ModuleType.Module6 => "isso_mod6.png",
            ModuleType.Module7 => "isso_mod7.png",
            _ => "isso_mod5.png"
        };

        moduleImage.Source = new BitmapImage(new Uri($"pack://application:,,,/Resources/{imageName}", UriKind.Absolute));
    }

    private void cbGebruikersModule_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (moduleImage == null) return;
        ApplyModuleLayout();
    }

    private void btnBereken_Click(object sender, RoutedEventArgs e)
    {
        // Validate required fluid constants
        if (!TryGetDouble(i5_9.Text, out double rho) || rho <= 0)
        {
            MessageBox.Show("Soortelijke massa (ρw) moet een geldig positief getal zijn.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryGetDouble(i5_10.Text, out double cw) || cw <= 0)
        {
            MessageBox.Show("Soortelijke warmte (cw) moet een geldig positief getal zijn.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validate required inputs: Φ and all temperatures (θ1-θ4)
        if (!TryGetDouble(i5_1.Text, out double phi) || phi <= 0)
        {
            MessageBox.Show("Thermisch vermogen (Φ) moet ingevuld zijn en positief.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryGetDouble(i5_5.Text, out double t1))
        {
            MessageBox.Show("Ingang bypass (θ1) moet ingevuld zijn.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryGetDouble(i5_6.Text, out double t2))
        {
            MessageBox.Show("Ingang hoofdlijn (θ2) moet ingevuld zijn.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryGetDouble(i5_7.Text, out double t3))
        {
            MessageBox.Show("Uitgang hoofdlijn (θ3) moet ingevuld zijn.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        bool hasBypass = SelectedModule != ModuleType.Module6;

        try
        {
            // Calculate qv1 from thermal power: Φ = (qv1 / 3600) × ρw × cw × (θ2 - θ3)
            double tempDiff = t2 - t3;
            if (Math.Abs(tempDiff) < 0.01)
            {
                MessageBox.Show("Temperatuurverschil (θ2 - θ3) is te klein. Kies grotere temperaturen.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double qv1 = (phi * 3600.0) / (rho * cw * tempDiff);

            if (!IsFinite(qv1) || qv1 <= 0)
            {
                MessageBox.Show("Berekende qv1 is ongeldig.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            i5_2.Text = FormatDouble(qv1);

            // Calculate flow rates based on module type
            double qv5, qv2;

            if (hasBypass)
            {
                // Module 5 and 7: bypass configuration
                // Energy balance: qv5 × θ1 + qv2 × θ3 = qv1 × θ2
                // Mass balance: qv5 + qv2 = qv1
                // Solution: qv5 = qv1 × (θ2 - θ3) / (θ1 - θ3)
                //          qv2 = qv1 × (θ1 - θ2) / (θ1 - θ3)

                double tempDiffBypass = t1 - t3;
                if (Math.Abs(tempDiffBypass) < 0.01)
                {
                    MessageBox.Show("Temperatuurverschil (θ1 - θ3) is te klein voor bypass-berekening.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                qv5 = qv1 * (t2 - t3) / tempDiffBypass;
                qv2 = qv1 * (t1 - t2) / tempDiffBypass;

                if (!IsFinite(qv5) || qv5 < 0 || !IsFinite(qv2) || qv2 < 0)
                {
                    MessageBox.Show("Berekende qv5 of qv2 is ongeldig. Controleer temperatuurverhoudingen.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                i5_4.Text = FormatDouble(qv5);
                i5_3.Text = FormatDouble(qv2);
            }
            else
            {
                // Module 6: no bypass
                i5_4.Text = "0";
                i5_3.Text = "0";
            }

            // θ4 = θ3 (outlet temperature follows outlet main line)
            i5_8.Text = FormatDouble(t3);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Berekening mislukt: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void btnWissen_Click(object sender, RoutedEventArgs e)
    {
        i5_1.Clear();
        i5_2.Clear();
        i5_3.Clear();
        i5_4.Clear();
        i5_5.Clear();
        i5_6.Clear();
        i5_7.Clear();
        i5_8.Clear();
        i5_9.Text = "981";
        i5_10.Text = "4,19";
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb) tb.SelectAll();
    }

    private void btnInfo_Click(object sender, RoutedEventArgs e)
    {
        InfoWindow info = new InfoWindow { Owner = this };
        info.ShowDialog();
    }

    private void btnAfsluiten_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
