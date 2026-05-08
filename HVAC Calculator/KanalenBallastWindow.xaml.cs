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

public partial class KanalenBallastWindow : Window
{
    public ObservableCollection<KanalenBallastRow> BallastRows { get; } = new();
    public ObservableCollection<string> AvailableShapes { get; } = new();

    private bool _isRecalculating;
    private bool _isViewReady;

    public KanalenBallastWindow()
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

        Loaded += (_, _) =>
        {
            WindowStateManager.RegisterWindow(this);
            if (Owner != null)
            {
                Left = Owner.Left + (Owner.ActualWidth - ActualWidth) / 2;
                Top = Owner.Top + Owner.ActualHeight - ActualHeight;
            }

            LoadShapes();
            AddRow();
            _isViewReady = true;
            RecalculateTotals();
        };
    }

    private void LoadShapes()
    {
        AvailableShapes.Clear();
        foreach (string shape in KanalenBallastCatalog.GetChannelShapes())
        {
            AvailableShapes.Add(shape);
        }
    }

    private void AttachRowEvents(KanalenBallastRow row)
    {
        row.PropertyChanged += (_, args) =>
        {
            if ((args.PropertyName == nameof(KanalenBallastRow.Shape) || args.PropertyName == nameof(KanalenBallastRow.SizeMm))
                && string.Equals(row.Shape, "Rond", StringComparison.Ordinal))
            {
                row.HeightMm = row.SizeMm;
            }

            if (_isViewReady && !_isRecalculating)
            {
                RecalculateTotals();
            }
        };
    }

    private void AddRow(KanalenBallastRow? sourceRow = null)
    {
        var row = new KanalenBallastRow
        {
            Shape = sourceRow?.Shape ?? AvailableShapes.FirstOrDefault() ?? "Rond",
            SizeMm = sourceRow?.SizeMm ?? "200",
            HeightMm = sourceRow?.HeightMm ?? "200",
            LengthM = sourceRow?.LengthM ?? "10"
        };

        if (string.Equals(row.Shape, "Rond", StringComparison.Ordinal))
        {
            row.HeightMm = row.SizeMm;
        }

        AttachRowEvents(row);
        BallastRows.Add(row);
        UpdateRowNumbers();

        if (_isViewReady)
        {
            ballastGrid.Dispatcher.BeginInvoke(() =>
            {
                ballastGrid.CurrentCell = new DataGridCellInfo(row, ballastGrid.Columns[4]);
                ballastGrid.BeginEdit();
                SelectActiveEditor();
            });
        }
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

                bool okLength = TryParseNumber(row.LengthM, out double lengthM) && lengthM >= 0;
                bool okSize = TryParseNumber(row.SizeMm, out double sizeMm) && sizeMm > 0;
                bool parsedHeight = TryParseNumber(row.HeightMm, out double heightMmParsed);
                bool okHeight = string.Equals(row.Shape, "Rond", StringComparison.Ordinal)
                    || (parsedHeight && heightMmParsed > 0);

                if (!okLength || !okSize || !okHeight)
                {
                    continue;
                }

                totalLengthM += lengthM;

                double heightMm = string.Equals(row.Shape, "Rond", StringComparison.Ordinal)
                    ? 0
                    : heightMmParsed;

                double kgPerM = KanalenBallastCatalog.GetWeightPerM(row.Shape, sizeMm, heightMm);
                double rowWeight = kgPerM * lengthM;

                row.WeightPerM = kgPerM.ToString("0.00", CultureInfo.InvariantCulture);
                row.RowWeightKg = rowWeight.ToString("0.00", CultureInfo.InvariantCulture);

                totalWeightKg += rowWeight;
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
        var sourceRow = (sender as FrameworkElement)?.DataContext as KanalenBallastRow;
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
            MessageBox.Show("Minimaal een kanaaldeel is vereist.", "Melding", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if ((sender as FrameworkElement)?.DataContext is KanalenBallastRow row)
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
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
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
        string description = "Kanalen ballast: kies kanaalvorm, maat en lengte. Gewichten worden bepaald op basis van de ronde tabel en rechthoekige plaatstaalbenadering uit de meegeleverde referentie.";
        InfoWindow info = new InfoWindow(description) { Owner = this };
        info.ShowDialog();
    }

    private void btnExport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Exporteer kanalen ballast naar CSV",
            Filter = "CSV-bestand (*.csv)|*.csv",
            DefaultExt = ".csv",
            FileName = "kanalen_ballast_export.csv"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var lines = new List<string>
        {
            "Kanaaldeel;Kanaalvorm;Maat [mm];Hoogte [mm];Lengte [m];Gewicht [kg/m];Gewicht [kg]"
        };

        foreach (var row in BallastRows)
        {
            lines.Add(string.Join(";",
                EscapeCsvValue(row.RowNumber.ToString(CultureInfo.InvariantCulture)),
                EscapeCsvValue(row.Shape),
                EscapeCsvValue(row.SizeMm),
                EscapeCsvValue(row.HeightMm),
                EscapeCsvValue(row.LengthM),
                EscapeCsvValue(row.WeightPerM),
                EscapeCsvValue(row.RowWeightKg)));
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

public class KanalenBallastRow : INotifyPropertyChanged
{
    private int _rowNumber;
    private string _shape = "Rond";
    private string _sizeMm = string.Empty;
    private string _heightMm = string.Empty;
    private string _lengthM = string.Empty;
    private string _weightPerM = string.Empty;
    private string _rowWeightKg = string.Empty;

    public int RowNumber
    {
        get => _rowNumber;
        set => SetField(ref _rowNumber, value);
    }

    public string Shape
    {
        get => _shape;
        set
        {
            if (SetField(ref _shape, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRectangular)));
            }
        }
    }

    public bool IsRectangular => string.Equals(_shape, "Rechthoekig", StringComparison.Ordinal);

    public string SizeMm
    {
        get => _sizeMm;
        set => SetField(ref _sizeMm, value);
    }

    public string HeightMm
    {
        get => _heightMm;
        set => SetField(ref _heightMm, value);
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

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
