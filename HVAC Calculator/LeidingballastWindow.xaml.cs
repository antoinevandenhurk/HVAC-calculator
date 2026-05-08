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
using HVACCalculator.Models;
using Microsoft.Win32;

namespace HVACCalculator;

public partial class LeidingballastWindow : Window
{
    public ObservableCollection<BallastRow> BallastRows { get; } = new();
    public ObservableCollection<string> AvailableMaterials { get; } = new();

    private readonly List<PipeBallastSelection> _allSelections = [];

    private bool _isRecalculating;
    private bool _isViewReady;

    public LeidingballastWindow()
    {
        InitializeComponent();
        DataContext = this;

        PreviewKeyDown += Window_PreviewKeyDown;

        ballastGrid.KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Tab && ballastGrid.CurrentCell.Column != null)
            {
                ballastGrid.Dispatcher.BeginInvoke(() => SelectActiveEditor());
            }
        };

        Loaded += (s, e) =>
        {
            WindowStateManager.RegisterWindow(this);
            if (Owner != null)
            {
                Left = Owner.Left + (Owner.ActualWidth - ActualWidth) / 2;
                Top = Owner.Top + Owner.ActualHeight - ActualHeight;
            }

            LoadSelectionTable();
            AddRow();
            _isViewReady = true;
            RecalculateTotals();
        };
    }

    private void LoadSelectionTable()
    {
        _allSelections.Clear();
        _allSelections.AddRange(PipeBallastCatalog.GetAllSelections());

        AvailableMaterials.Clear();
        foreach (var materialName in _allSelections.Select(selection => selection.MaterialName).Distinct().OrderBy(name => name))
        {
            AvailableMaterials.Add(materialName);
        }
    }

    private void AttachRowEvents(BallastRow row)
    {
        row.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(BallastRow.SelectedMaterial))
            {
                UpdateDiameterOptions(row, row.SelectedPipe?.OuterDiameterMm);
            }

            if (_isViewReady && !_isRecalculating)
            {
                RecalculateTotals();
            }
        };
    }

    private void AddRow(BallastRow? sourceRow = null)
    {
        var row = new BallastRow
        {
            SelectedMaterial = sourceRow?.SelectedMaterial ?? AvailableMaterials.FirstOrDefault() ?? string.Empty,
            LengthM = sourceRow?.LengthM ?? "10"
        };

        AttachRowEvents(row);
        UpdateDiameterOptions(row, sourceRow?.SelectedPipe?.OuterDiameterMm);
        BallastRows.Add(row);
        UpdateRowNumbers();

        if (_isViewReady)
        {
            ballastGrid.Dispatcher.BeginInvoke(() =>
            {
                ballastGrid.CurrentCell = new DataGridCellInfo(row, ballastGrid.Columns[3]);
                ballastGrid.BeginEdit();
                SelectActiveEditor();
            });
        }
    }

    private void UpdateDiameterOptions(BallastRow row, double? preferredOuterDiameterMm)
    {
        row.AvailableDiameters.Clear();

        foreach (var selection in _allSelections.Where(selection => string.Equals(selection.MaterialName, row.SelectedMaterial, StringComparison.Ordinal)).OrderBy(selection => selection.OuterDiameterMm))
        {
            row.AvailableDiameters.Add(selection);
        }

        if (row.AvailableDiameters.Count == 0)
        {
            row.SelectedPipe = null;
            return;
        }

        if (preferredOuterDiameterMm.HasValue)
        {
            var preferred = row.AvailableDiameters.FirstOrDefault(item => Math.Abs(item.OuterDiameterMm - preferredOuterDiameterMm.Value) < 0.05);
            if (preferred != null)
            {
                row.SelectedPipe = preferred;
                return;
            }
        }

        row.SelectedPipe = row.AvailableDiameters.FirstOrDefault();
    }

    private void UpdateRowNumbers()
    {
        for (int i = 0; i < BallastRows.Count; i++)
        {
            BallastRows[i].RowNumber = i + 1;
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

    private void RecalculateTotals()
    {
        if (!_isViewReady || _isRecalculating)
        {
            return;
        }

        _isRecalculating = true;
        try
        {
            double totalLengthM = 0;
            double totalWeightKg = 0;

            foreach (var row in BallastRows)
            {
                row.WeightPerM = string.Empty;
                row.RowWeightKg = string.Empty;

                if (!TryParseNumber(row.LengthM, out double lengthM) || lengthM < 0)
                {
                    continue;
                }

                totalLengthM += lengthM;

                if (row.SelectedPipe == null)
                {
                    continue;
                }

                double kgPerM = row.SelectedPipe.FilledKgPerM;

                double rowWeight = lengthM * kgPerM;
                totalWeightKg += rowWeight;
                row.WeightPerM = kgPerM.ToString("0.00", CultureInfo.InvariantCulture);
                row.RowWeightKg = rowWeight.ToString("0.00", CultureInfo.InvariantCulture);
            }

            tbTotalLength.Text = totalLengthM.ToString("0.00", CultureInfo.InvariantCulture);
            tbTotalWeight.Text = totalWeightKg.ToString("0.00", CultureInfo.InvariantCulture);
        }
        finally
        {
            _isRecalculating = false;
        }
    }

    private void AddRow_Click(object sender, RoutedEventArgs e)
    {
        var sourceRow = (sender as FrameworkElement)?.DataContext as BallastRow;
        AddRow(sourceRow);
    }

    private void RemoveLastRow()
    {
        if (BallastRows.Count <= 1)
        {
            return;
        }

        BallastRows.RemoveAt(BallastRows.Count - 1);
        UpdateRowNumbers();
        RecalculateTotals();
    }

    private void RemoveRow_Click(object sender, RoutedEventArgs e)
    {
        if (BallastRows.Count <= 1)
        {
            MessageBox.Show("Minimaal een leidingdeel is vereist.", "Melding", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if ((sender as FrameworkElement)?.DataContext is BallastRow row)
        {
            BallastRows.Remove(row);
            UpdateRowNumbers();
            RecalculateTotals();
        }
    }

    private void btnWissen_Click(object sender, RoutedEventArgs e)
    {
        if (!_isViewReady)
        {
            return;
        }

        BallastRows.Clear();
        AddRow();
        RecalculateTotals();
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            tb.SelectAll();
        }
    }

    private void ComboBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            comboBox.IsDropDownOpen = true;
        }
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.OemPlus || e.Key == System.Windows.Input.Key.Add)
        {
            AddRow(BallastRows.LastOrDefault());
            e.Handled = true;
            return;
        }

        if (e.Key == System.Windows.Input.Key.OemMinus || e.Key == System.Windows.Input.Key.Subtract)
        {
            RemoveLastRow();
            e.Handled = true;
        }
    }

    private void SelectActiveEditor()
    {
        if (System.Windows.Input.Keyboard.FocusedElement is TextBox textBox)
        {
            textBox.SelectAll();
            return;
        }

        if (System.Windows.Input.Keyboard.FocusedElement is ComboBox comboBox)
        {
            comboBox.IsDropDownOpen = true;
        }
    }

    private void btnInfo_Click(object sender, RoutedEventArgs e)
    {
        string description = "Leidingballast: kies per leidingdeel materiaal, diameter en lengte. Gewichten zijn gebaseerd op tabellen per materiaal en worden automatisch opgeteld.";
        InfoWindow info = new InfoWindow(description) { Owner = this };
        info.ShowDialog();
    }

    private void btnExport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Exporteer leidingballast naar CSV",
            Filter = "CSV-bestand (*.csv)|*.csv",
            DefaultExt = ".csv",
            FileName = "leidingballast_export.csv"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var lines = new List<string>
        {
            "Leidingdeel;Leidingmateriaal;Diameter [mm];Lengte [m];Gewicht [kg/m];Gewicht [kg]"
        };

        foreach (var row in BallastRows)
        {
            string material = row.SelectedMaterial ?? string.Empty;
            string diameter = row.SelectedPipe?.OuterDiameterMm.ToString("0.#", CultureInfo.InvariantCulture) ?? string.Empty;
            string length = row.LengthM ?? string.Empty;
            string kgPerM = row.WeightPerM ?? string.Empty;
            string kg = row.RowWeightKg ?? string.Empty;

            lines.Add(string.Join(";",
                EscapeCsvValue(row.RowNumber.ToString(CultureInfo.InvariantCulture)),
                EscapeCsvValue(material),
                EscapeCsvValue(diameter),
                EscapeCsvValue(length),
                EscapeCsvValue(kgPerM),
                EscapeCsvValue(kg)));
        }

        File.WriteAllLines(dialog.FileName, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        MessageBox.Show("CSV-export voltooid.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string EscapeCsvValue(string value)
    {
        string safe = value.Replace("\"", "\"\"");
        return $"\"{safe}\"";
    }

    private void btnAfsluiten_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

public class BallastRow : INotifyPropertyChanged
{
    private int _rowNumber;
    private string _selectedMaterial = string.Empty;
    private PipeBallastSelection? _selectedPipe;
    private string _lengthM = string.Empty;
    private string _weightPerM = string.Empty;
    private string _rowWeightKg = string.Empty;

    public ObservableCollection<PipeBallastSelection> AvailableDiameters { get; } = new();

    public int RowNumber
    {
        get => _rowNumber;
        set => SetField(ref _rowNumber, value);
    }

    public PipeBallastSelection? SelectedPipe
    {
        get => _selectedPipe;
        set => SetField(ref _selectedPipe, value);
    }

    public string SelectedMaterial
    {
        get => _selectedMaterial;
        set => SetField(ref _selectedMaterial, value);
    }

    public string LengthM
    {
        get => _lengthM;
        set => SetField(ref _lengthM, value);
    }

    public string WeightPerM
    {
        get => _weightPerM;
        set => SetField(ref _weightPerM, value);
    }

    public string RowWeightKg
    {
        get => _rowWeightKg;
        set => SetField(ref _rowWeightKg, value);
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
