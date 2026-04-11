using System.Windows;
using System.Windows.Controls;

namespace HVACCalculator;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (s, e) => WindowStateManager.RegisterWindow(this);
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private void btnBerekenDebietOpp_Click(object sender, RoutedEventArgs e)
    {
        // Implementatie nodig
        MessageBox.Show("Bereken debiet (Oppervlakte) - Implementatie nodig", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnVerwijderen_Click(object sender, RoutedEventArgs e)
    {
        // Implementatie nodig
        MessageBox.Show("Verwijderen - Implementatie nodig", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnBerekenDebietArea_Click(object sender, RoutedEventArgs e)
    {
        // Implementatie nodig
        MessageBox.Show("Bereken debiet (Area) - Implementatie nodig", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnBerekenSnelheidOpp_Click(object sender, RoutedEventArgs e)
    {
        // Implementatie nodig
        MessageBox.Show("Bereken snelheid (Oppervlakte) - Implementatie nodig", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnBerekenDebietRond_Click(object sender, RoutedEventArgs e)
    {
        // Implementatie nodig
        MessageBox.Show("Bereken debiet (Rond) - Implementatie nodig", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnBerekenSnelheidRond_Click(object sender, RoutedEventArgs e)
    {
        // Implementatie nodig
        MessageBox.Show("Bereken snelheid (Rond) - Implementatie nodig", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnBerekenDebietBxH_Click(object sender, RoutedEventArgs e)
    {
        // Implementatie nodig
        MessageBox.Show("Bereken debiet (B x H) - Implementatie nodig", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnBerekenSnelheidBxH_Click(object sender, RoutedEventArgs e)
    {
        // Implementatie nodig
        MessageBox.Show("Bereken snelheid (B x H) - Implementatie nodig", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnAfsluiten_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void btnInfo_Click(object sender, RoutedEventArgs e)
    {
        InfoWindow infoWindow = new InfoWindow();
        infoWindow.Owner = this;
        infoWindow.ShowDialog();
    }
}
