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
    private static void SelectMaterial(ComboBox comboBox, string materialName)
    {
        foreach (var item in comboBox.Items)
        {
            if (item is ComboBoxItem comboBoxItem && string.Equals(comboBoxItem.Content?.ToString(), materialName, StringComparison.Ordinal))
            {
                comboBox.SelectedItem = comboBoxItem;
                return;
            }
        }
    }

    public CVGKWWindow()
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
            SelectMaterial(cbLeidingMateriaal, AppSettings.PreferredMaterialCvgkw);
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

    public void PrefillFromGebruikersModule(double qv, double theta2, double theta3)
    {
        // Opened from users-module screen: prefill values and show immediate diameter suggestion.
        cbLeidingMateriaal.SelectedIndex = 0; // Dikwandige CV buis
        i2_1.Clear();
        i2_2.Text = FormatDouble(qv, "0.###");
        i2_3.Text = "981";
        i2_4.Text = "4,19";
        i2_5.Text = FormatDouble(theta2, "0.##");
        i2_6.Text = FormatDouble(theta3, "0.##");

        if (qv > 0)
        {
            UpdatePipeSuggestion(qv);
        }
        else
        {
            InitPipeTable();
        }
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
        string material = GetSelectedMaterialName();

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

    private string GetSelectedMaterialName()
    {
        return (cbLeidingMateriaal.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
    }

    private double GetFluidTemperatureC()
    {
        bool hasTheta2 = TryGetDouble(i2_5.Text, out double theta2);
        bool hasTheta3 = TryGetDouble(i2_6.Text, out double theta3);

        if (hasTheta2 && hasTheta3)
        {
            return (theta2 + theta3) / 2.0;
        }

        if (hasTheta2) return theta2;
        if (hasTheta3) return theta3;

        return 20.0;
    }

    private void UpdatePipeSuggestion(double qv)
    {
        var pipes = GetSelectedPipes();
        double minLinDp = AppSettings.MinVelocityCvgkw;
        double maxLinDp = AppSettings.MaxVelocityCvgkw;
        string material = GetSelectedMaterialName();
        double roughnessMm = CopperPipe.GetRoughnessMm(material);
        double fluidTemperatureC = GetFluidTemperatureC();
        double density = CopperPipe.GetWaterDensity(fluidTemperatureC);
        double kinematicViscosity = CopperPipe.GetWaterKinematicViscosity(fluidTemperatureC);
        const double targetResistance = 150.0;

        var evaluated = pipes
            .Select(p =>
            {
                double velocity = p.Velocity(qv);
                double resistance = p.ResistanceFromFlow(qv, roughnessMm, density, kinematicViscosity);
                return new { Pipe = p, Velocity = velocity, Resistance = resistance };
            })
            .ToList();

        var filtered = evaluated
            .Where(x => x.Resistance >= minLinDp && x.Resistance <= maxLinDp)
            .OrderByDescending(x => x.Resistance)
            .ToList();

        // Aanbevolen = regel die het dichtst bij doelweerstand ligt binnen de band.
        var aanbevolen = filtered
            .OrderBy(x => Math.Abs(x.Resistance - targetResistance))
            .ThenByDescending(x => x.Resistance)
            .Select(x => x.Pipe)
            .FirstOrDefault();

        var rows = filtered.Select(x =>
        {
            var p = x.Pipe;
            double v = x.Velocity;
            double weerstand = x.Resistance;
            bool isAanbevolen = p == aanbevolen;
            return new PipeTableRow
            {
                Omschrijving      = string.IsNullOrEmpty(p.Omschrijving) ? p.Name : p.Omschrijving,
                Buitendiameter    = p.OuterDiameter.ToString("0.#"),
                Wanddikte         = p.WallThickness.ToString("0.#"),
                InwendigeDiameter = p.InnerDiameter.ToString("0.#"),
                Snelheid          = v.ToString("0.00"),
                Weerstandswaarde = weerstand.ToString("0.0"),
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
            Weerstandswaarde = "",
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
    public string Weerstandswaarde { get; set; } = "";
    public bool   IsAanbevolen      { get; set; }
}
