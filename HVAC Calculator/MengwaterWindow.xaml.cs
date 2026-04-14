using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace HVACCalculator;

public partial class MengwaterWindow : Window
{
    public MengwaterWindow()
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
        };
    }

    private static bool TryGetDouble(string text, out double value)
    {
        return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value);
    }

    private static string FormatDouble(double value)
    {
        return value.ToString("0.###", CultureInfo.CurrentCulture);
    }

    private void btnBereken_Click(object sender, RoutedEventArgs e)
    {
        bool qv1Filled = TryGetDouble(i4_1.Text, out double qv1);
        bool t1Filled = TryGetDouble(i4_2.Text, out double t1);
        bool qv2Filled = TryGetDouble(i4_3.Text, out double qv2);
        bool t2Filled = TryGetDouble(i4_4.Text, out double t2);
        bool qv3Filled = TryGetDouble(i4_5.Text, out double qv3);
        bool t3Filled = TryGetDouble(i4_6.Text, out double t3);

        int emptyCount = (qv1Filled ? 0 : 1)
            + (t1Filled ? 0 : 1)
            + (qv2Filled ? 0 : 1)
            + (t2Filled ? 0 : 1)
            + (qv3Filled ? 0 : 1)
            + (t3Filled ? 0 : 1);

        if (emptyCount == 0)
        {
            MessageBox.Show("Alle velden zijn ingevuld. Laat precies 1 veld leeg om te berekenen.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (emptyCount > 1)
        {
            MessageBox.Show("Er zijn te veel lege velden. Vul alle velden behalve 1 in.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (qv1Filled && qv1 <= 0 || qv2Filled && qv2 <= 0 || qv3Filled && qv3 <= 0)
        {
            MessageBox.Show("Volumestromen qv1, qv2 en qv3 moeten positief zijn.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (!qv1Filled)
            {
                if (t1 == 0)
                {
                    MessageBox.Show("θ1 mag niet 0 zijn bij berekening van qv1.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                qv1 = ((qv3 * t3) - (qv2 * t2)) / t1;
                if (qv1 <= 0)
                {
                    MessageBox.Show("Berekende qv1 is niet positief. Controleer de invoerwaarden.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                i4_1.Text = FormatDouble(qv1);
            }
            else if (!t1Filled)
            {
                if (qv1 == 0)
                {
                    MessageBox.Show("qv1 mag niet 0 zijn bij berekening van θ1.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                t1 = ((qv3 * t3) - (qv2 * t2)) / qv1;
                i4_2.Text = FormatDouble(t1);
            }
            else if (!qv2Filled)
            {
                if (t2 == 0)
                {
                    MessageBox.Show("θ2 mag niet 0 zijn bij berekening van qv2.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                qv2 = ((qv3 * t3) - (qv1 * t1)) / t2;
                if (qv2 <= 0)
                {
                    MessageBox.Show("Berekende qv2 is niet positief. Controleer de invoerwaarden.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                i4_3.Text = FormatDouble(qv2);
            }
            else if (!t2Filled)
            {
                if (qv2 == 0)
                {
                    MessageBox.Show("qv2 mag niet 0 zijn bij berekening van θ2.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                t2 = ((qv3 * t3) - (qv1 * t1)) / qv2;
                i4_4.Text = FormatDouble(t2);
            }
            else if (!qv3Filled)
            {
                if (t3 == 0)
                {
                    MessageBox.Show("θ3 mag niet 0 zijn bij berekening van qv3.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                qv3 = ((qv1 * t1) + (qv2 * t2)) / t3;
                if (qv3 <= 0)
                {
                    MessageBox.Show("Berekende qv3 is niet positief. Controleer de invoerwaarden.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                i4_5.Text = FormatDouble(qv3);
            }
            else if (!t3Filled)
            {
                if (qv3 == 0)
                {
                    MessageBox.Show("qv3 mag niet 0 zijn bij berekening van θ3.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                t3 = ((qv1 * t1) + (qv2 * t2)) / qv3;
                i4_6.Text = FormatDouble(t3);
            }
        }
        catch (Exception)
        {
            MessageBox.Show("Berekening mislukt door ongeldige invoercombinatie.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void btnWissen_Click(object sender, RoutedEventArgs e)
    {
        i4_1.Clear();
        i4_2.Clear();
        i4_3.Clear();
        i4_4.Clear();
        i4_5.Clear();
        i4_6.Clear();
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
