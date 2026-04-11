using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HVACCalculator.Models;

namespace HVACCalculator;

public partial class CVGKWWindow : Window
{
    public CVGKWWindow()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            if (Owner != null)
            {
                Left = Owner.Left + (Owner.ActualWidth - ActualWidth) / 2;
                Top = Owner.Top + Owner.ActualHeight - ActualHeight;
            }
            InitPipeTable();
        };
    }

    private static bool TryGetDouble(string text, out double value)
    {
        return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value);
    }

    private static string FormatDouble(double value, string format)
    {
        return value.ToString(format, CultureInfo.CurrentCulture);
    }

    private void btnBereken_Click(object sender, RoutedEventArgs e)
    {
        bool phi_filled = TryGetDouble(i2_1.Text, out double phi);
        bool qv_filled = TryGetDouble(i2_2.Text, out double qv);
        bool rho_filled = TryGetDouble(i2_3.Text, out double rho);
        bool cw_filled = TryGetDouble(i2_4.Text, out double cw);
        bool theta2_filled = TryGetDouble(i2_5.Text, out double theta2);
        bool theta3_filled = TryGetDouble(i2_6.Text, out double theta3);

        // Soortelijke massa en warmte moeten altijd ingevuld zijn
        if (!rho_filled || !cw_filled)
        {
            MessageBox.Show("Soortelijke massa (ρw) en soortelijke warmte (cw) moeten ingevuld zijn.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Controleer welke velden leeg zijn
        bool phi_empty = !phi_filled;
        bool qv_empty = !qv_filled;
        bool theta2_empty = !theta2_filled;
        bool theta3_empty = !theta3_filled;

        int emptyCount = (phi_empty ? 1 : 0) + (qv_empty ? 1 : 0) + (theta2_empty ? 1 : 0) + (theta3_empty ? 1 : 0);

        if (emptyCount == 0)
        {
            MessageBox.Show("Alle velden zijn ingevuld. Er is niets om te berekenen.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        else if (emptyCount > 1)
        {
            MessageBox.Show("Er zijn te veel lege velden. Vul alle velden behalve één in.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Controleer of waarden positief zijn voor ingevulde velden
        if (phi_filled && phi <= 0) { MessageBox.Show("Waterzijdig vermogen moet positief zijn.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (qv_filled && qv <= 0) { MessageBox.Show("Volumestroom moet positief zijn.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (rho <= 0) { MessageBox.Show("Soortelijke massa moet positief zijn.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (cw <= 0) { MessageBox.Show("Soortelijke warmte moet positief zijn.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        // Bereken het ontbrekende veld
        if (phi_empty)
        {
            // Φ [kW] = (qv [m3/h] / 3600) * ρw * Cw * (θ2 - θ3)
            double deltaT = theta2 - theta3;
            if (deltaT <= 0)
            {
                MessageBox.Show("Temperatuurverschil (θ2 - θ3) moet positief zijn.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            phi = (qv / 3600.0) * rho * cw * deltaT;
            i2_1.Text = FormatDouble(phi, "0.###");
            UpdatePipeSuggestion(qv);
        }
        else if (qv_empty)
        {
            // qv [m3/h] = (Φ [kW] / (ρw * Cw * (θ2 - θ3))) * 3600
            double deltaT = theta2 - theta3;
            if (deltaT <= 0)
            {
                MessageBox.Show("Temperatuurverschil (θ2 - θ3) moet positief zijn.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            qv = (phi / (rho * cw * deltaT)) * 3600.0;
            i2_2.Text = FormatDouble(qv, "0.##");
            UpdatePipeSuggestion(qv);
        }
        else if (theta2_empty)
        {
            // θ2 = Φ [kW] / ((qv [m3/h] / 3600) * ρw * Cw) + θ3
            double denominator = (qv / 3600.0) * rho * cw;
            if (denominator == 0)
            {
                MessageBox.Show("Kan θ2 niet berekenen - ongeldige combinatie van waarden.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            theta2 = (phi / denominator) + theta3;
            i2_5.Text = FormatDouble(theta2, "0.##");
            UpdatePipeSuggestion(qv);
        }
        else if (theta3_empty)
        {
            // θ3 = θ2 - (Φ [kW] / ((qv [m3/h] / 3600) * ρw * Cw))
            double denominator = (qv / 3600.0) * rho * cw;
            if (denominator == 0)
            {
                MessageBox.Show("Kan θ3 niet berekenen - ongeldige combinatie van waarden.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            theta3 = theta2 - (phi / denominator);
            i2_6.Text = FormatDouble(theta3, "0.##");
            UpdatePipeSuggestion(qv);
        }
    }

    private void btnWissen_Click(object sender, RoutedEventArgs e)
    {
        i2_1.Clear();
        i2_2.Clear();
        i2_5.Clear();
        i2_6.Clear();
        // Reset standaardwaarden
        i2_3.Text = "981";
        i2_4.Text = "4,19";
        InitPipeTable();
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
        this.Close();
    }

    private List<CopperPipe> GetSelectedPipes()
    {
        string material = (cbLeidingMateriaal.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

        return material switch
        {
            "Koperen buis"        => CopperPipe.GetStandardSizes(),
            "Dunwandige CV buis"  => CopperPipe.GetThinWallSizes(),
            "Dikwandige CV buis"  => CopperPipe.GetThickWallSizes(),
            "Henco buis"          => CopperPipe.GetHencoSizes(),
            "PE SDR11 buis"       => CopperPipe.GetPeSDR11Sizes(),
            _                     => CopperPipe.GetStandardSizes()
        };
    }

    private void UpdatePipeSuggestion(double qv)
    {
        var pipes = GetSelectedPipes();
        const double minVelocity = 0.8;
        const double maxVelocity = 2.0;

        var filtered = pipes
            .Select(p => new { Pipe = p, Velocity = p.Velocity(qv) })
            .Where(x => x.Velocity >= minVelocity && x.Velocity <= maxVelocity)
            .ToList();

        // Aanbevolen = kleinste buis waarbij snelheid ≤ 1,0 m/s
        var aanbevolen = filtered.Select(x => x.Pipe).FirstOrDefault(p => p.Velocity(qv) <= 1.0);

        var rows = filtered.Select(x =>
        {
            var p = x.Pipe;
            double v = x.Velocity;
            bool isAanbevolen = p == aanbevolen;
            return new PipeTableRow
            {
                Omschrijving      = string.IsNullOrEmpty(p.Omschrijving) ? p.Name : p.Omschrijving,
                Buitendiameter    = p.OuterDiameter.ToString("0.#"),
                Wanddikte         = p.WallThickness.ToString("0.#"),
                InwendigeDiameter = p.InnerDiameter.ToString("0.#"),
                Snelheid          = v.ToString("0.00"),
                IsAanbevolen      = isAanbevolen
            };
        }).ToList();

        pipeGrid.ItemsSource = rows;
    }

    private void InitPipeTable()
    {
        var pipes = GetSelectedPipes();
        pipeGrid.ItemsSource = pipes.Select(p => new PipeTableRow
        {
            Omschrijving      = string.IsNullOrEmpty(p.Omschrijving) ? p.Name : p.Omschrijving,
            Buitendiameter    = p.OuterDiameter.ToString("0.#"),
            Wanddikte         = p.WallThickness.ToString("0.#"),
            InwendigeDiameter = p.InnerDiameter.ToString("0.#"),
            Snelheid          = "",
            IsAanbevolen      = false
        }).ToList();
    }

    private void cbLeidingMateriaal_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (pipeTableTitle == null || pipeGrid == null) return;

        pipeTableTitle.Text = "Leidingselectie: " + ((cbLeidingMateriaal.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "-");

        if (TryGetDouble(i2_2.Text, out double qv) && qv > 0)
        {
            UpdatePipeSuggestion(qv);
        }
        else
        {
            InitPipeTable();
        }
    }
}

public class PipeTableRow
{
    public string Omschrijving      { get; set; } = "";
    public string Buitendiameter    { get; set; } = "";
    public string Wanddikte         { get; set; } = "";
    public string InwendigeDiameter { get; set; } = "";
    public string Snelheid          { get; set; } = "";
    public bool   IsAanbevolen      { get; set; }
}
