using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace HVACCalculator;

public partial class WarmteverliesAfgifteVuistregelWindow : Window
{
    private const double ScreedLambdaWmK = 1.4;
    // EN 1264-2: surface resistances for upward (heating) and downward (cooling) heat flow.
    private const double SurfaceResistanceHeatingM2kW = 0.125;
    private const double SurfaceResistanceCoolingM2kW = 0.175;

    public ObservableCollection<WarmteverliesAfgifteRow> RuleRows { get; } = new();
    public ObservableCollection<string> FloorCoveringOptions { get; } = new()
        { "Steen", "Tapijt", "Linoleum", "Parket" };

    private static double GetFloorCoveringRc(string? coveringType)
    {
        return coveringType switch
        {
            "Steen"    => 0.02,
            "Tapijt"   => 0.09,
            "Linoleum" => 0.02,
            "Parket"   => 0.07,
            _          => 0.02
        };
    }

    private bool _isRecalculating;
    private bool _isViewReady;

    public WarmteverliesAfgifteVuistregelWindow()
    {
        InitializeComponent();
        DataContext = this;

        Loaded += Window_Loaded;
        tbRelativeHumidity.TextChanged += (_, _) => RecalculateTotals();
        tbSafetyMargin.TextChanged += (_, _) => RecalculateTotals();
        tbCoolingRoomTemp.TextChanged += (_, _) => RecalculateTotals();
        tbCoolingSupplyTemp.TextChanged += (_, _) => RecalculateTotals();
        tbCoolingReturnTemp.TextChanged += (_, _) => RecalculateTotals();

        vuistregelGrid.KeyDown += (_, keyEvent) =>
        {
            if (keyEvent.Key == System.Windows.Input.Key.OemPlus || keyEvent.Key == System.Windows.Input.Key.Add)
            {
                AddRow(RuleRows.LastOrDefault());
                keyEvent.Handled = true;
            }
            else if (keyEvent.Key == System.Windows.Input.Key.OemMinus || keyEvent.Key == System.Windows.Input.Key.Subtract)
            {
                RemoveLastRow();
                keyEvent.Handled = true;
            }
        };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        WindowStateManager.RegisterWindow(this);
        if (Owner != null)
        {
            Left = Owner.Left + (Owner.ActualWidth - ActualWidth) / 2;
            Top = Owner.Top + Owner.ActualHeight - ActualHeight;
        }

        AddRow();
        _isViewReady = true;
        RecalculateTotals();
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

    private static double ClampNonNegative(double value) => value < 0 ? 0 : value;

    private double GetBuildingHeatLossFactorWm2()
    {
        int index = cbIsolatieNivo.SelectedIndex;
        return index switch
        {
            0 => 90.0,
            1 => 60.0,
            2 => 40.0,
            3 => 25.0,
            _ => 60.0
        };
    }

    private static double CalculateDewPointC(double airTempC, double relativeHumidityPercent)
    {
        // Magnus approximation (sufficient for HVAC rule-of-thumb checks).
        double rh = Math.Clamp(relativeHumidityPercent, 1.0, 100.0);
        const double a = 17.27;
        const double b = 237.7;
        double gamma = ((a * airTempC) / (b + airTempC)) + Math.Log(rh / 100.0);
        return (b * gamma) / (a - gamma);
    }

    // EN 1264-2: equivalent screed thickness accounts for HOH pipe spacing and pipe diameter.
    // Returns (kHeating [W/m2K], kCooling [W/m2K]).
    private static (double kHeating, double kCooling) CalculateEffectiveK(
        double spacingMm, double diameterMm, double screedThicknessMm, double floorCoveringRc)
    {
        if (spacingMm <= 0 || diameterMm <= 0 || screedThicknessMm <= 0)
            return (0, 0);

        // EN 1264-2 eq. 10: equivalent screed thickness for pipe layout
        double logArg = spacingMm / (Math.PI * diameterMm);
        if (logArg <= 0) return (0, 0);
        double equivThicknessMm = screedThicknessMm + spacingMm / (2.0 * Math.PI) * Math.Log(logArg);
        double rScreedEff = equivThicknessMm / (1000.0 * ScreedLambdaWmK);

        double rBase = rScreedEff + floorCoveringRc;
        double rTotalHeating = rBase + SurfaceResistanceHeatingM2kW;
        double rTotalCooling = rBase + SurfaceResistanceCoolingM2kW;

        double kHeating = rTotalHeating > 0 ? 1.0 / rTotalHeating : 0;
        double kCooling = rTotalCooling > 0 ? 1.0 / rTotalCooling : 0;
        return (kHeating, kCooling);
    }

    private void AttachRowEvents(WarmteverliesAfgifteRow row)
    {
        row.PropertyChanged += (_, _) =>
        {
            if (_isViewReady && !_isRecalculating)
            {
                RecalculateTotals();
            }
        };
    }

    private void AddRow(WarmteverliesAfgifteRow? sourceRow = null)
    {
        var row = new WarmteverliesAfgifteRow
        {
            LengthM = sourceRow?.LengthM ?? "1",
            WidthM = sourceRow?.WidthM ?? "1",
            SupplyTempC = sourceRow?.SupplyTempC ?? "35",
            ReturnTempC = sourceRow?.ReturnTempC ?? "30",
            RoomTempC = sourceRow?.RoomTempC ?? "20",
            SpacingMm = sourceRow?.SpacingMm ?? "150",
            DiameterMm = sourceRow?.DiameterMm ?? "16",
            ScreedThicknessMm = sourceRow?.ScreedThicknessMm ?? "60",
            InsulationThicknessMm = sourceRow?.InsulationThicknessMm ?? "20",
            FloorCoveringType = sourceRow?.FloorCoveringType ?? "Steen"
        };

        AttachRowEvents(row);
        RuleRows.Add(row);
        UpdateRowNumbers();

        if (_isViewReady)
        {
            vuistregelGrid.Dispatcher.BeginInvoke(() =>
            {
                vuistregelGrid.CurrentCell = new DataGridCellInfo(row, vuistregelGrid.Columns[1]);
                vuistregelGrid.BeginEdit();
            });
        }
    }

    private void RemoveLastRow()
    {
        if (RuleRows.Count <= 1)
        {
            return;
        }

        RuleRows.RemoveAt(RuleRows.Count - 1);
        UpdateRowNumbers();
        RecalculateTotals();
    }

    private void UpdateRowNumbers()
    {
        for (int i = 0; i < RuleRows.Count; i++)
        {
            RuleRows[i].RowNumber = i + 1;
        }
    }

    private void RecalculateTotals()
    {
        if (!_isViewReady || _isRecalculating)
        {
            return;
        }

        _isRecalculating = true;
        try
        {
            double factorWm2 = GetBuildingHeatLossFactorWm2();

            bool okRh = TryParseNumber(tbRelativeHumidity.Text, out double relativeHumidityPercent);
            if (!okRh)
            {
                relativeHumidityPercent = 60.0;
            }

            bool okMargin = TryParseNumber(tbSafetyMargin.Text, out double safetyMarginC);
            if (!okMargin)
            {
                safetyMarginC = 1.5;
            }

            bool okTvCool = TryParseNumber(tbCoolingRoomTemp.Text, out double tvCoolC);
            if (!okTvCool)
            {
                tvCoolC = 25.0;
            }

            bool okTaCool = TryParseNumber(tbCoolingSupplyTemp.Text, out double taCoolC);
            if (!okTaCool)
            {
                taCoolC = 18.0;
            }

            bool okTrCool = TryParseNumber(tbCoolingReturnTemp.Text, out double trCoolC);
            if (!okTrCool)
            {
                trCoolC = 21.0;
            }

            double tMeanCoolC = (taCoolC + trCoolC) / 2.0;

            int condensationRiskCount = 0;
            double totalArea = 0;
            double totalHeatLoss = 0;
            double totalHeatingOutput = 0;
            double totalCoolingOutput = 0;

            foreach (var row in RuleRows)
            {
                row.HeatLossW = string.Empty;
                row.HeatingOutputWm2 = string.Empty;
                row.HeatingOutputTotalW = string.Empty;
                row.CoolingOutputWm2 = string.Empty;
                row.CoolingOutputTotalW = string.Empty;
                row.CondensationWarning = string.Empty;

                bool okLength = TryParseNumber(row.LengthM, out double lengthM) && lengthM > 0;
                bool okWidth = TryParseNumber(row.WidthM, out double widthM) && widthM > 0;
                bool okTa = TryParseNumber(row.SupplyTempC, out double taC);
                bool okTr = TryParseNumber(row.ReturnTempC, out double trC);
                bool okTv = TryParseNumber(row.RoomTempC, out double tvC);
                bool okS = TryParseNumber(row.SpacingMm, out double spacingMm) && spacingMm > 0;
                bool okD = TryParseNumber(row.DiameterMm, out double diameterMm) && diameterMm > 0;
                bool okVd = TryParseNumber(row.ScreedThicknessMm, out double screedThicknessMm) && screedThicknessMm > 0;
                bool okId = TryParseNumber(row.InsulationThicknessMm, out double insulationThicknessMm) && insulationThicknessMm >= 0;

                if (!(okLength && okWidth && okTa && okTr && okTv && okS && okD && okVd && okId))
                {
                    continue;
                }

                double areaM2 = lengthM * widthM;
                double heatLossW = areaM2 * factorWm2;

                double tMeanHeatC = (taC + trC) / 2.0;
                double rc = GetFloorCoveringRc(row.FloorCoveringType);
                var (kHeating, kCooling) = CalculateEffectiveK(spacingMm, diameterMm, screedThicknessMm, rc);

                double waWm2 = ClampNonNegative(kHeating * (tMeanHeatC - tvC));
                double kaWm2 = ClampNonNegative(kCooling * (tvCoolC - tMeanCoolC));

                double wtW = waWm2 * areaM2;
                double ktW = kaWm2 * areaM2;

                double dewPointC = CalculateDewPointC(tvCoolC, relativeHumidityPercent);
                double limitC = dewPointC + safetyMarginC;
                bool condensationRisk = tMeanCoolC < limitC;
                if (condensationRisk)
                {
                    condensationRiskCount++;
                }

                if (kaWm2 <= 0.0001)
                {
                    row.CondensationWarning = condensationRisk ? "Geen koeling / condensrisico" : "Geen koeling (Tvk<=Tmkoel)";
                }
                else if (condensationRisk)
                {
                    row.CondensationWarning = "Condensrisico";
                }

                row.HeatLossW = heatLossW.ToString("0.0", CultureInfo.InvariantCulture);
                row.HeatingOutputWm2 = waWm2.ToString("0.0", CultureInfo.InvariantCulture);
                row.HeatingOutputTotalW = wtW.ToString("0.0", CultureInfo.InvariantCulture);
                row.CoolingOutputWm2 = kaWm2.ToString("0.0", CultureInfo.InvariantCulture);
                row.CoolingOutputTotalW = ktW.ToString("0.0", CultureInfo.InvariantCulture);

                totalArea += areaM2;
                totalHeatLoss += heatLossW;
                totalHeatingOutput += wtW;
                totalCoolingOutput += ktW;
            }

            tbTotalArea.Text = totalArea.ToString("0.00", CultureInfo.InvariantCulture);
            tbTotalHeatLoss.Text = totalHeatLoss.ToString("0.0", CultureInfo.InvariantCulture);
            tbTotalHeatingOutput.Text = totalHeatingOutput.ToString("0.0", CultureInfo.InvariantCulture);
            tbTotalCoolingOutput.Text = totalCoolingOutput.ToString("0.0", CultureInfo.InvariantCulture);

            tbCondensationSummary.Text = condensationRiskCount > 0
                ? $"Condensatiewaarschuwing in {condensationRiskCount} regel(s): controleer Tm, RH en veiligheidsmarge."
                : string.Empty;
        }
        finally
        {
            _isRecalculating = false;
        }
    }

    private void cbIsolatieNivo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RecalculateTotals();
    }

    private void AddRow_Click(object sender, RoutedEventArgs e)
    {
        var sourceRow = (sender as FrameworkElement)?.DataContext as WarmteverliesAfgifteRow;
        AddRow(sourceRow);
    }

    private void RemoveRow_Click(object sender, RoutedEventArgs e)
    {
        if (RuleRows.Count <= 1)
        {
            MessageBox.Show("Minimaal een ruimte is vereist.", "Melding", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if ((sender as FrameworkElement)?.DataContext is WarmteverliesAfgifteRow row)
        {
            RuleRows.Remove(row);
            UpdateRowNumbers();
            RecalculateTotals();
        }
    }

    private void btnWissen_Click(object sender, RoutedEventArgs e)
    {
        RuleRows.Clear();
        AddRow();
        RecalculateTotals();
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private void btnInfo_Click(object sender, RoutedEventArgs e)
    {
        InfoWindow info = new InfoWindow("Globale vuistregel voor warmteverlies per ruimte en indicatieve warmte- en koelafgifte van vloerverwarming/vloerkoeling.") { Owner = this };
        info.ShowDialog();
    }

    private void btnExport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Exporteer warmteverlies/afgifte naar CSV",
            Filter = "CSV-bestand (*.csv)|*.csv",
            DefaultExt = ".csv",
            FileName = "warmteverlies_afgifte_export.csv"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var lines = new List<string>
        {
            "Ruimte;Lengte [m];Breedte [m];Warmteverlies [W];Ta [C];Tr [C];Tv [C];s [mm];d [mm];Afwerking;Vd [mm];Id [mm];Wa [W/m2];Wt [W];Ka [W/m2];Kt [W];Waarschuwing"
        };

        foreach (var row in RuleRows)
        {
            lines.Add(string.Join(";",
                EscapeCsvValue(row.RowNumber.ToString(CultureInfo.InvariantCulture)),
                EscapeCsvValue(row.LengthM),
                EscapeCsvValue(row.WidthM),
                EscapeCsvValue(row.HeatLossW),
                EscapeCsvValue(row.SupplyTempC),
                EscapeCsvValue(row.ReturnTempC),
                EscapeCsvValue(row.RoomTempC),
                EscapeCsvValue(row.SpacingMm),
                EscapeCsvValue(row.DiameterMm),
                EscapeCsvValue(row.FloorCoveringType),
                EscapeCsvValue(row.ScreedThicknessMm),
                EscapeCsvValue(row.InsulationThicknessMm),
                EscapeCsvValue(row.HeatingOutputWm2),
                EscapeCsvValue(row.HeatingOutputTotalW),
                EscapeCsvValue(row.CoolingOutputWm2),
                EscapeCsvValue(row.CoolingOutputTotalW),
                EscapeCsvValue(row.CondensationWarning)));
        }

        File.WriteAllLines(dialog.FileName, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        MessageBox.Show("CSV-export voltooid.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string EscapeCsvValue(string? value)
    {
        string safe = (value ?? string.Empty).Replace("\"", "\"\"");
        return $"\"{safe}\"";
    }

    private void btnAfsluiten_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

public class WarmteverliesAfgifteRow : INotifyPropertyChanged
{
    private int _rowNumber;
    private string _lengthM = string.Empty;
    private string _widthM = string.Empty;
    private string _heatLossW = string.Empty;
    private string _supplyTempC = string.Empty;
    private string _returnTempC = string.Empty;
    private string _roomTempC = string.Empty;
    private string _spacingMm = string.Empty;
    private string _diameterMm = string.Empty;
    private string _screedThicknessMm = string.Empty;
    private string _insulationThicknessMm = string.Empty;
    private string _floorCoveringType = "Steen";
    private string _heatingOutputWm2 = string.Empty;
    private string _heatingOutputTotalW = string.Empty;
    private string _coolingOutputWm2 = string.Empty;
    private string _coolingOutputTotalW = string.Empty;
    private string _condensationWarning = string.Empty;

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

    public string WidthM
    {
        get => _widthM;
        set => SetField(ref _widthM, value);
    }

    public string HeatLossW
    {
        get => _heatLossW;
        set => SetField(ref _heatLossW, value);
    }

    public string SupplyTempC
    {
        get => _supplyTempC;
        set => SetField(ref _supplyTempC, value);
    }

    public string ReturnTempC
    {
        get => _returnTempC;
        set => SetField(ref _returnTempC, value);
    }

    public string RoomTempC
    {
        get => _roomTempC;
        set => SetField(ref _roomTempC, value);
    }

    public string SpacingMm
    {
        get => _spacingMm;
        set => SetField(ref _spacingMm, value);
    }

    public string DiameterMm
    {
        get => _diameterMm;
        set => SetField(ref _diameterMm, value);
    }

    public string ScreedThicknessMm
    {
        get => _screedThicknessMm;
        set => SetField(ref _screedThicknessMm, value);
    }

    public string InsulationThicknessMm
    {
        get => _insulationThicknessMm;
        set => SetField(ref _insulationThicknessMm, value);
    }

    public string FloorCoveringType
    {
        get => _floorCoveringType;
        set => SetField(ref _floorCoveringType, value);
    }

    public string HeatingOutputWm2
    {
        get => _heatingOutputWm2;
        set => SetField(ref _heatingOutputWm2, value);
    }

    public string HeatingOutputTotalW
    {
        get => _heatingOutputTotalW;
        set => SetField(ref _heatingOutputTotalW, value);
    }

    public string CoolingOutputWm2
    {
        get => _coolingOutputWm2;
        set => SetField(ref _coolingOutputWm2, value);
    }

    public string CoolingOutputTotalW
    {
        get => _coolingOutputTotalW;
        set => SetField(ref _coolingOutputTotalW, value);
    }

    public string CondensationWarning
    {
        get => _condensationWarning;
        set => SetField(ref _condensationWarning, value);
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
