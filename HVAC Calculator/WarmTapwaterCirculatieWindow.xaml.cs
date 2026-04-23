using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using HVACCalculator.Models;

namespace HVACCalculator;

public partial class WarmTapwaterCirculatieWindow : Window
{
    private const double MinimumReturnTemperatureC = 60.0;
    private const double WaterSpecificHeatKJkgK = 4.19;

    public ObservableCollection<RecirculationRow> RecirculationRows { get; } = new();
    public ObservableCollection<string> AvailableDiameters { get; } = new();
    public ObservableCollection<string> AvailableInsulationThicknesses { get; } = new()
    {
        "0", "10", "15", "20", "25", "30", "40", "50"
    };
    private bool _isRecalculating;
    private bool _isViewReady;


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

    public WarmTapwaterCirculatieWindow()
    {
        try
        {
            InitializeComponent();
            DataContext = this;
            
            Loaded += Window_Loaded;
            tbWaterTempC.TextChanged += TbWaterTempC_TextChanged;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fout bij initializatie: {ex}", "Kritieke fout", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    private void TbWaterTempC_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_isViewReady && !_isRecalculating && tbWaterTempC != null)
        {
            RecalculateTotals(showValidationErrors: false);
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            WindowStateManager.RegisterWindow(this);
            if (Owner != null)
            {
                Left = Owner.Left + (Owner.ActualWidth - ActualWidth) / 2;
                Top = Owner.Top + Owner.ActualHeight - ActualHeight;
            }

            if (recircGrid != null)
            {
                recircGrid.KeyDown += (_, keyEvent) =>
                {
                    if (keyEvent?.Key == System.Windows.Input.Key.OemPlus || keyEvent?.Key == System.Windows.Input.Key.Add)
                    {
                        var lastRow = RecirculationRows?.LastOrDefault();
                        AddRow(lastRow);
                        keyEvent.Handled = true;
                    }
                };
            }

            if (cbLeidingMateriaal != null)
            {
                SelectMaterial(cbLeidingMateriaal, AppSettings.PreferredMaterialTapwater);
                UpdateDiameterOptions();
            }
            
            if (AvailableDiameters.Count == 0)
            {
                AvailableDiameters.Add("16");
            }
            
            AddRow();
            _isViewReady = true;
            RecalculateTotals(showValidationErrors: false);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fout bij laden venster: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private List<CopperPipe> GetSelectedPipes()
    {
        string material = GetSelectedMaterialName();
        return material switch
        {
            "Koperen buis" => CopperPipe.GetStandardSizes(),
            "Dunwandige CV buis" => CopperPipe.GetThinWallSizes(),
            "Dikwandige CV buis" => CopperPipe.GetThickWallSizes(),
            "Henco buis" => CopperPipe.GetHencoSizes(),
            "PE SDR11 buis" => CopperPipe.GetPeSDR11Sizes(),
            _ => CopperPipe.GetStandardSizes()
        };
    }

    private string GetSelectedMaterialName()
    {
        return (cbLeidingMateriaal.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
    }

    private void UpdateDiameterOptions()
    {
        try
        {
            var diameters = GetSelectedPipes()
                .Select(p => p.OuterDiameter.ToString("0.#", CultureInfo.InvariantCulture))
                .Distinct()
                .ToList();

            if (AvailableDiameters != null)
            {
                AvailableDiameters.Clear();
                foreach (var diameter in diameters)
                {
                    AvailableDiameters.Add(diameter);
                }
            }

            string fallback = AvailableDiameters?.FirstOrDefault() ?? "16";
            if (RecirculationRows != null)
            {
                foreach (var row in RecirculationRows)
                {
                    if (row != null && (string.IsNullOrWhiteSpace(row.OuterDiameterMm) || !(AvailableDiameters?.Contains(row.OuterDiameterMm) ?? false)))
                    {
                        row.OuterDiameterMm = fallback;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fout bij diameter update: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateRowNumbers()
    {
        for (int i = 0; i < RecirculationRows.Count; i++)
        {
            RecirculationRows[i].RowNumber = i + 1;
        }
    }

    private void AttachRowEvents(RecirculationRow row)
    {
        if (row == null) return;
        row.PropertyChanged += (_, __) =>
        {
            if (!_isRecalculating && _isViewReady)
            {
                RecalculateTotals(showValidationErrors: false);
            }
        };
    }

    private void AddRow(RecirculationRow? sourceRow = null)
    {
        try
        {
            var row = new RecirculationRow
            {
                LengthM = sourceRow?.LengthM ?? "10",
                OuterDiameterMm = sourceRow?.OuterDiameterMm ?? (AvailableDiameters?.FirstOrDefault() ?? "16"),
                InsulationThicknessMm = sourceRow?.InsulationThicknessMm ?? "20",
                AmbientTempC = sourceRow?.AmbientTempC ?? "20"
            };

            AttachRowEvents(row);
            if (RecirculationRows != null)
            {
                RecirculationRows.Add(row);
            }
            UpdateRowNumbers();

            if (_isViewReady && recircGrid != null)
            {
                try
                {
                    recircGrid.CurrentCell = new System.Windows.Controls.DataGridCellInfo(row, recircGrid.Columns[1]);
                    recircGrid.Focus();
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fout bij toevoegen rij: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddRow_Click(object sender, RoutedEventArgs e)
    {
        var sourceRow = (sender as FrameworkElement)?.DataContext as RecirculationRow;
        AddRow(sourceRow);
    }

    private void RemoveRow_Click(object sender, RoutedEventArgs e)
    {
        if (RecirculationRows.Count <= 1)
        {
            MessageBox.Show("Minimaal een recirculatieregel is vereist.", "Melding", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if ((sender as FrameworkElement)?.DataContext is RecirculationRow row)
        {
            RecirculationRows.Remove(row);
            UpdateRowNumbers();
            RecalculateTotals(showValidationErrors: false);
        }
    }

    private static bool TryParseNumber(string? text, out double value)
    {
        value = 0;
        string raw = text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
            || double.TryParse(raw.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private bool RecalculateTotals(bool showValidationErrors)
    {
        try
        {
            if (!_isViewReady || tbTotalHeatLoss == null || tbRecirculationFlow == null || tbTotalPressureLoss == null || tbTemperatureDrop == null || tbWaterTempC == null)
            {
                return false;
            }

            if (_isRecalculating)
            {
                return false;
            }

            _isRecalculating = true;
            double totalHeatLoss = 0;
            double totalPressureLoss = 0;
            var selectedPipes = GetSelectedPipes();
            string material = GetSelectedMaterialName();
            double roughnessMm = CopperPipe.GetRoughnessMm(material);
            
            if (!TryParseNumber(tbWaterTempC.Text, out double supplyWaterTempC) || supplyWaterTempC <= MinimumReturnTemperatureC)
            {
                if (showValidationErrors)
                {
                    MessageBox.Show("Leidingwatertemperatuur moet hoger zijn dan 60 °C.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                tbTotalHeatLoss.Text = string.Empty;
                tbRecirculationFlow.Text = string.Empty;
                tbTemperatureDrop.Text = string.Empty;
                tbTotalPressureLoss.Text = string.Empty;
                return false;
            }

            double designDeltaT = supplyWaterTempC - MinimumReturnTemperatureC;

            var parsedRows = new List<(RecirculationRow Row, CopperPipe Pipe, double LengthM, double InsulationThicknessMm, double AmbientTempC, double Ub, double RowHeatLoss)>();

            for (int i = 0; i < RecirculationRows.Count; i++)
            {
                var row = RecirculationRows[i];

                bool okLength = TryParseNumber(row.LengthM, out double lengthM) && lengthM >= 0;
                bool okDiameter = TryParseNumber(row.OuterDiameterMm, out double outerDiameterMm) && outerDiameterMm > 0;
                bool okInsThickness = TryParseNumber(row.InsulationThicknessMm, out double insulationThicknessMm) && insulationThicknessMm >= 0;
                bool okAmbientTemp = TryParseNumber(row.AmbientTempC, out double ambientTempC);

                if (!okLength || !okDiameter || !okInsThickness || !okAmbientTemp)
                {
                    row.RowHeatLossW = "";
                    row.PipeLambda = "";
                    row.InsulationLambda = "";
                    row.UbValue = "";
                    row.Velocity = "";
                    row.IsVelocityTooHigh = false;
                    row.PressureLossPerM = "";
                    if (showValidationErrors)
                    {
                        MessageBox.Show($"Controleer regel {i + 1}: vul lengte, diameter, isolatiedikte en omgevingstemperatuur met geldige waarden in.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    tbTotalHeatLoss.Text = string.Empty;
                    tbRecirculationFlow.Text = string.Empty;
                    tbTemperatureDrop.Text = string.Empty;
                    tbTotalPressureLoss.Text = string.Empty;
                    return false;
                }

                var pipe = selectedPipes.FirstOrDefault(p => Math.Abs(p.OuterDiameter - outerDiameterMm) < 0.05);
                if (pipe == null)
                {
                    row.RowHeatLossW = "";
                    row.PipeLambda = "";
                    row.InsulationLambda = "";
                    row.UbValue = "";
                    row.Velocity = "";
                    row.IsVelocityTooHigh = false;
                    row.PressureLossPerM = "";
                    if (showValidationErrors)
                    {
                        MessageBox.Show($"Regel {i + 1}: leidingdiameter komt niet voor in de gekozen leidingselectie.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    tbTotalHeatLoss.Text = string.Empty;
                    tbRecirculationFlow.Text = string.Empty;
                    tbTemperatureDrop.Text = string.Empty;
                    tbTotalPressureLoss.Text = string.Empty;
                    return false;
                }

                double meanTempC = (supplyWaterTempC + ambientTempC) / 2.0;
                double pipeLambda = GetPipeConductivity(material, meanTempC);
                double insulationLambda = GetInsulationConductivity(meanTempC);

                double effectiveInsulationThicknessMm = MapInsulationThickness(insulationThicknessMm);
                double ub = CalculateUb(pipe, effectiveInsulationThicknessMm, pipeLambda, insulationLambda);
                double deltaT = Math.Abs(supplyWaterTempC - ambientTempC);

                // ISSO-formule 5.62 zonder equivalente lengte: Φ = l × U_b × ΔT
                double rowHeatLoss = lengthM * ub * deltaT;
                totalHeatLoss += rowHeatLoss;

                row.PipeLambda = pipeLambda.ToString("0.000", CultureInfo.InvariantCulture);
                row.InsulationLambda = insulationLambda.ToString("0.000", CultureInfo.InvariantCulture);
                row.UbValue = ub.ToString("0.000", CultureInfo.InvariantCulture);
                row.RowHeatLossW = rowHeatLoss.ToString("0.0", CultureInfo.InvariantCulture);

                parsedRows.Add((row, pipe, lengthM, insulationThicknessMm, ambientTempC, ub, rowHeatLoss));
            }

            double meanWaterTempC = supplyWaterTempC;
            double density = CopperPipe.GetWaterDensity(meanWaterTempC);
            double kinematicViscosity = CopperPipe.GetWaterKinematicViscosity(meanWaterTempC);

            // ISSO recirculatie volumestroom uit warmteverlies met retourvoorwaarde >= 60°C.
            double totalHeatLossKW = totalHeatLoss / 1000.0;
            double qvM3h = 0;
            if (density > 0 && WaterSpecificHeatKJkgK > 0 && designDeltaT > 0)
            {
                qvM3h = (totalHeatLossKW / (density * WaterSpecificHeatKJkgK * designDeltaT)) * 3600.0;
            }

            double qvLs = qvM3h / 3.6;
            double actualDeltaT = 0;
            if (qvM3h > 0 && density > 0 && WaterSpecificHeatKJkgK > 0)
            {
                actualDeltaT = (totalHeatLossKW * 3600.0) / (density * WaterSpecificHeatKJkgK * qvM3h);
            }

            if (supplyWaterTempC - actualDeltaT < MinimumReturnTemperatureC - 0.001)
            {
                if (showValidationErrors)
                {
                    MessageBox.Show("De berekende retourtemperatuur komt onder 60 °C uit. Verhoog de leidingwatertemperatuur.", "Waarschuwing", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                tbTotalHeatLoss.Text = totalHeatLoss.ToString("0.0", CultureInfo.InvariantCulture);
                tbRecirculationFlow.Text = qvLs.ToString("0.000", CultureInfo.InvariantCulture);
                tbTemperatureDrop.Text = actualDeltaT.ToString("0.00", CultureInfo.InvariantCulture);
                tbTotalPressureLoss.Text = string.Empty;
                return false;
            }

            foreach (var item in parsedRows)
            {
                double velocity = item.Pipe.Velocity(qvM3h);
                double pressureLossPerM = item.Pipe.ResistanceFromFlow(qvM3h, roughnessMm, density, kinematicViscosity);
                totalPressureLoss += pressureLossPerM * item.LengthM;

                item.Row.Velocity = velocity.ToString("0.00", CultureInfo.InvariantCulture);
                item.Row.IsVelocityTooHigh = velocity > 0.7;
                item.Row.PressureLossPerM = pressureLossPerM.ToString("0.0", CultureInfo.InvariantCulture);
            }

            tbTotalHeatLoss.Text = totalHeatLoss.ToString("0.0", CultureInfo.InvariantCulture);
            tbRecirculationFlow.Text = qvLs.ToString("0.000", CultureInfo.InvariantCulture);
            tbTemperatureDrop.Text = actualDeltaT.ToString("0.00", CultureInfo.InvariantCulture);
            tbTotalPressureLoss.Text = totalPressureLoss.ToString("0.0", CultureInfo.InvariantCulture);
            return true;
        }
        finally
        {
            _isRecalculating = false;
        }
    }

    private static double GetPipeConductivity(string material, double meanTempC)
    {
        // Indicatieve selectie op basis van materiaalsoort volgens tabel-benadering.
        _ = meanTempC;
        return material switch
        {
            "Koperen buis" => 385.0,
            "Dunwandige CV buis" => 50.0,
            "Dikwandige CV buis" => 50.0,
            "Henco buis" => 0.43,
            "PE SDR11 buis" => 0.40,
            _ => 50.0
        };
    }

    private static double GetInsulationConductivity(double meanTempC)
    {
        // Lineaire benadering rond minerale-wol referentiewaarden (tabel 5.42).
        double lambda = 0.035 + Math.Max(0, meanTempC - 20.0) * 0.0002;
        return Math.Clamp(lambda, 0.032, 0.050);
    }

    private static double MapInsulationThickness(double selectedThicknessMm)
    {
        return selectedThicknessMm switch
        {
            0 => 0,
            10 => 10,
            15 => 15,
            20 => 20,
            25 => 25,
            30 => 30,
            40 => 40,
            50 => 50,
            _ => 20
        };
    }

    private static double CalculateUb(CopperPipe pipe, double insulationThicknessMm, double pipeLambda, double insulationLambda)
    {
        double innerRadiusM = pipe.InnerDiameter / 2000.0;
        double pipeOuterRadiusM = pipe.OuterDiameter / 2000.0;
        double insulationOuterRadiusM = (pipe.OuterDiameter + (2.0 * insulationThicknessMm)) / 2000.0;

        if (innerRadiusM <= 0 || pipeOuterRadiusM <= innerRadiusM || insulationOuterRadiusM < pipeOuterRadiusM)
        {
            return 0;
        }

        double resistancePipe = Math.Log(pipeOuterRadiusM / innerRadiusM) / (2.0 * Math.PI * pipeLambda);
        double resistanceInsulation = insulationThicknessMm > 0
            ? Math.Log(insulationOuterRadiusM / pipeOuterRadiusM) / (2.0 * Math.PI * insulationLambda)
            : 0.0;

        double totalResistance = resistancePipe + resistanceInsulation;
        return totalResistance > 0 ? 1.0 / totalResistance : 0;
    }

    private void btnWissen_Click(object sender, RoutedEventArgs e)
    {
        if (!_isViewReady)
        {
            return;
        }

        RecirculationRows.Clear();
        AddRow();
        tbTotalHeatLoss.Clear();
        tbRecirculationFlow.Clear();
        tbTemperatureDrop.Clear();
        tbTotalPressureLoss.Clear();
        RecalculateTotals(showValidationErrors: false);
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            tb.SelectAll();
        }
    }

    private void cbLeidingMateriaal_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (!_isViewReady || cbLeidingMateriaal == null)
            {
                return;
            }

            UpdateDiameterOptions();
            RecalculateTotals(showValidationErrors: false);
        }
        catch (Exception)
        {
            // Log error silently to avoid cascading crashes
        }
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

public class RecirculationRow : INotifyPropertyChanged
{
    private int _rowNumber;
    private string _lengthM = string.Empty;
    private string _outerDiameterMm = string.Empty;
    private string _insulationThicknessMm = string.Empty;
    private string _ambientTempC = string.Empty;
    private string _pipeLambda = string.Empty;
    private string _insulationLambda = string.Empty;
    private string _ubValue = string.Empty;
    private string _velocity = string.Empty;
    private bool _isVelocityTooHigh;
    private string _pressureLossPerM = string.Empty;
    private string _rowHeatLossW = string.Empty;

    public int RowNumber
    {
        get => _rowNumber;
        set => SetField(ref _rowNumber, value);
    }

    public string LengthM
    {
        get => _lengthM;
        set => SetField(ref _lengthM, value);
    }

    public string OuterDiameterMm
    {
        get => _outerDiameterMm;
        set => SetField(ref _outerDiameterMm, value);
    }

    public string InsulationThicknessMm
    {
        get => _insulationThicknessMm;
        set => SetField(ref _insulationThicknessMm, value);
    }

    public string AmbientTempC
    {
        get => _ambientTempC;
        set => SetField(ref _ambientTempC, value);
    }

    public string PipeLambda
    {
        get => _pipeLambda;
        set => SetField(ref _pipeLambda, value);
    }

    public string InsulationLambda
    {
        get => _insulationLambda;
        set => SetField(ref _insulationLambda, value);
    }

    public string UbValue
    {
        get => _ubValue;
        set => SetField(ref _ubValue, value);
    }

    public string Velocity
    {
        get => _velocity;
        set => SetField(ref _velocity, value);
    }

    public bool IsVelocityTooHigh
    {
        get => _isVelocityTooHigh;
        set => SetField(ref _isVelocityTooHigh, value);
    }

    public string PressureLossPerM
    {
        get => _pressureLossPerM;
        set => SetField(ref _pressureLossPerM, value);
    }

    public string RowHeatLossW
    {
        get => _rowHeatLossW;
        set => SetField(ref _rowHeatLossW, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
