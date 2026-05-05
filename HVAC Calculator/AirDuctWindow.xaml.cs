using System;
using System.Windows;
using System.Windows.Controls;

namespace HVACCalculator;

public partial class AirDuctWindow : Window
{
    public AirDuctWindow()
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

    private static bool TryGetPositiveDouble(string text, out double value)
    {
        if (double.TryParse(text, out value) && value > 0) return true;
        value = 0; return false;
    }

    private bool TryGetArea(out double area)
    {
        if (TryGetPositiveDouble(i1_4.Text, out double diameter))
        {
            area = Math.PI * Math.Pow(diameter / 2000.0, 2);
            return true;
        }
        if (TryGetPositiveDouble(i1_5.Text, out double width) && TryGetPositiveDouble(i1_6.Text, out double height))
        {
            area = (width / 1000.0) * (height / 1000.0);
            return true;
        }
        return TryGetPositiveDouble(i1_3.Text, out area);
    }

    private void UpdateDerivedFieldsFromArea(double area)
    {
        i1_3.Text = Math.Round(area, 4).ToString();
        double diameter = Math.Sqrt(4 * area / Math.PI) * 1000;
        i1_4.Text = Math.Round(diameter, 0).ToString();

        if (TryGetPositiveDouble(i1_5.Text, out double width))
            i1_6.Text = Math.Round(area * 1_000_000 / width, 0).ToString();
        else if (TryGetPositiveDouble(i1_6.Text, out double height))
            i1_5.Text = Math.Round(area * 1_000_000 / height, 0).ToString();
        else
        {
            double side = Math.Sqrt(area * 1_000_000);
            i1_5.Text = i1_6.Text = Math.Round(side, 0).ToString();
        }
    }

    private void btnBerekenDebietOpp_Click(object sender, RoutedEventArgs e)
    {
        if (TryGetPositiveDouble(i1_1.Text, out double q) && TryGetPositiveDouble(i1_2.Text, out double v))
            UpdateDerivedFieldsFromArea(q / (v * 3600));
    }

    private void btnBerekenDebietArea_Click(object sender, RoutedEventArgs e)
    {
        if (TryGetPositiveDouble(i1_2.Text, out double v) && TryGetArea(out double area))
        {
            i1_1.Text = Math.Round(v * area * 3600, 0).ToString();
            UpdateDerivedFieldsFromArea(area);
        }
    }

    private void btnBerekenSnelheidOpp_Click(object sender, RoutedEventArgs e)
    {
        if (TryGetPositiveDouble(i1_1.Text, out double q) && TryGetArea(out double area))
        {
            i1_2.Text = Math.Round(q / (area * 3600), 0).ToString();
            UpdateDerivedFieldsFromArea(area);
        }
    }

    private void btnBerekenDebietRond_Click(object sender, RoutedEventArgs e)
    {
        if (TryGetPositiveDouble(i1_2.Text, out double v) && TryGetPositiveDouble(i1_4.Text, out double d))
        {
            double area = Math.PI * Math.Pow(d / 2000.0, 2);
            i1_1.Text = Math.Round(v * area * 3600, 0).ToString();
            UpdateDerivedFieldsFromArea(area);
        }
    }

    private void btnBerekenSnelheidRond_Click(object sender, RoutedEventArgs e)
    {
        if (TryGetPositiveDouble(i1_1.Text, out double q) && TryGetPositiveDouble(i1_4.Text, out double d))
        {
            double area = Math.PI * Math.Pow(d / 2000.0, 2);
            i1_2.Text = Math.Round(q / (area * 3600), 0).ToString();
            UpdateDerivedFieldsFromArea(area);
        }
    }

    private void btnBerekenDebietBxH_Click(object sender, RoutedEventArgs e)
    {
        if (TryGetPositiveDouble(i1_2.Text, out double v) && TryGetPositiveDouble(i1_5.Text, out double w) && TryGetPositiveDouble(i1_6.Text, out double h))
        {
            double area = (w / 1000.0) * (h / 1000.0);
            i1_1.Text = Math.Round(v * area * 3600, 0).ToString();
            UpdateDerivedFieldsFromArea(area);
        }
    }

    private void btnBerekenSnelheidBxH_Click(object sender, RoutedEventArgs e)
    {
        if (TryGetPositiveDouble(i1_1.Text, out double q) && TryGetPositiveDouble(i1_5.Text, out double w) && TryGetPositiveDouble(i1_6.Text, out double h))
        {
            double area = (w / 1000.0) * (h / 1000.0);
            i1_2.Text = Math.Round(q / (area * 3600), 0).ToString();
            UpdateDerivedFieldsFromArea(area);
        }
    }

    private void btnVerwijderen_Click(object sender, RoutedEventArgs e)
    {
        i1_1.Clear(); i1_2.Clear(); i1_3.Clear(); i1_4.Clear(); i1_5.Clear(); i1_6.Clear();
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb) tb.SelectAll();
    }

    private void btnInfo_Click(object sender, RoutedEventArgs e)
    {
        string airDuctInfo = "Snelselectie van kanalen op basis van stroomsnelheden. Voor DykaAir kanalen kan je equivalente kanaalmaten invoeren, namelijk; DykaAir 195x80 is ~Ø131 en 235x80 is ~Ø143.";
        InfoWindow info = new InfoWindow(airDuctInfo) { Owner = this };
        info.ShowDialog();
    }

    private void btnAfsluiten_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}