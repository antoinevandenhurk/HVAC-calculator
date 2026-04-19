using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace HVACCalculator;

public partial class SettingsWindow : Window
{
    private static void SelectMaterial(ComboBox comboBox, string materialName)
    {
        foreach (var item in comboBox.Items)
        {
            if (item is ComboBoxItem comboBoxItem && string.Equals(comboBoxItem.Content?.ToString(), materialName, System.StringComparison.Ordinal))
            {
                comboBox.SelectedItem = comboBoxItem;
                return;
            }
        }

        if (comboBox.Items.Count > 0)
        {
            comboBox.SelectedIndex = 0;
        }
    }

    private static string? GetSelectedMaterial(ComboBox comboBox)
    {
        return (comboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
    }

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
            SelectMaterial(cbPreferredMaterialCvgkw, AppSettings.PreferredMaterialCvgkw);
            SelectMaterial(cbPreferredMaterialTapwater, AppSettings.PreferredMaterialTapwater);
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
        string? preferredMaterialCvgkw = GetSelectedMaterial(cbPreferredMaterialCvgkw);
        string? preferredMaterialTapwater = GetSelectedMaterial(cbPreferredMaterialTapwater);

        if (!cvMinOk || !cvMaxOk || !tapMinOk || !tapMaxOk)
        {
            MessageBox.Show("Voer geldige positieve waarden in voor Lin Δp CV/GKW en alle stroomsnelheden.", "Fout",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(preferredMaterialCvgkw) || string.IsNullOrWhiteSpace(preferredMaterialTapwater))
        {
            MessageBox.Show("Selecteer een voorkeur leidingmateriaal voor CV/GKW en Tapwater.", "Fout",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (cvMin >= cvMax)
        {
            MessageBox.Show("Voor CV/GKW moet de minimale Lin Δp kleiner zijn dan de maximale Lin Δp.", "Fout",
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
        AppSettings.PreferredMaterialCvgkw = preferredMaterialCvgkw;
        AppSettings.PreferredMaterialTapwater = preferredMaterialTapwater;
        AppSettings.Save();

        Close();
    }

    private void btnSluiten_Click(object sender, RoutedEventArgs e) => Close();
}
