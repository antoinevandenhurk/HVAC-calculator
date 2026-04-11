using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HVACCalculator.Models;

namespace HVACCalculator;

public partial class TapwaterWindow : Window
{
    public TapwaterWindow()
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
            InitPipeTable();
        };
    }

    private static bool TryGetDouble(string text, out double value)
    {
        return double.TryParse(text, out value);
    }

    private void btnBereken_Click(object sender, RoutedEventArgs e)
    {
        // Formule: Qv [l/s] = MAX( (0,083 * √TE) + (0,417 * ∜SE) + Qcv , BSH + Qcv )

        if (!TryGetDouble(i3_1.Text, out double TE) || TE < 0)
        {
            MessageBox.Show("Som Tapeenheden (TE) moet een geldig positief getal zijn.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryGetDouble(i3_2.Text, out double SE) || SE < 0)
        {
            MessageBox.Show("Som Spoeleenheden (SE) moet een geldig positief getal zijn.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryGetDouble(i3_3.Text, out double Qcv) || Qcv < 0)
        {
            MessageBox.Show("Continu verbruik (Qcv) moet een geldig positief getal zijn.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryGetDouble(i3_4.Text, out double BSH) || BSH < 0)
        {
            MessageBox.Show("Aantal Brandslanghaspel (BSH) moet een geldig positief getal zijn.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Bereken BSH waarde: 1 BSH = 0,361 l/s, meer dan 1 = 0,72 l/s
        double BSH_value = BSH == 0 ? 0 : (BSH == 1 ? 0.361 : 0.72);

        // Bereken eerste deel: (0,083 * √TE) + (0,417 * ∜SE) + Qcv
        double sqrt_TE = Math.Sqrt(TE);
        double fourth_root_SE = Math.Pow(SE, 0.25); // ∜SE = SE^(1/4)
        double first_calculation = (0.083 * sqrt_TE) + (0.417 * fourth_root_SE) + Qcv;

        // Bereken tweede deel: BSH + Qcv
        double second_calculation = BSH_value + Qcv;

        // Neem de hoogste waarde
        double Qv = Math.Max(first_calculation, second_calculation);

        // Toon het resultaat
        i3_5.Text = Math.Round(Qv, 3).ToString();

        // Leidingselectie bijwerken (Qv l/s → m³/h)
        UpdatePipeSuggestion(Qv * 3.6);
    }

    private void btnWissen_Click(object sender, RoutedEventArgs e)
    {
        i3_1.Clear();
        i3_2.Text = "0";
        i3_3.Text = "0";
        i3_4.Text = "0";
        i3_5.Clear();
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
            "Koperen buis"       => CopperPipe.GetStandardSizes(),
            "Dunwandige CV buis" => CopperPipe.GetThinWallSizes(),
            "Dikwandige CV buis" => CopperPipe.GetThickWallSizes(),
            "Henco buis"         => CopperPipe.GetHencoSizes(),
            "PE SDR11 buis"      => CopperPipe.GetPeSDR11Sizes(),
            _                    => CopperPipe.GetStandardSizes()
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

        var aanbevolen = filtered.Select(x => x.Pipe).FirstOrDefault(p => p.Velocity(qv) <= 1.0);

        pipeGrid.ItemsSource = filtered.Select(x =>
        {
            var p = x.Pipe;
            double v = x.Velocity;
            return new PipeTableRow
            {
                Omschrijving      = string.IsNullOrEmpty(p.Omschrijving) ? p.Name : p.Omschrijving,
                Buitendiameter    = p.OuterDiameter.ToString("0.#"),
                Wanddikte         = p.WallThickness.ToString("0.#"),
                InwendigeDiameter = p.InnerDiameter.ToString("0.#"),
                Snelheid          = v.ToString("0.00"),
                IsAanbevolen      = p == aanbevolen
            };
        }).ToList();
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

        if (double.TryParse(i3_5.Text, out double qv_ls) && qv_ls > 0)
            UpdatePipeSuggestion(qv_ls * 3.6);
        else
            InitPipeTable();
    }
}
