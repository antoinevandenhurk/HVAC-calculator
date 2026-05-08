using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HVACCalculator.Models;

public sealed class PipeBallastSelection
{
    public string MaterialName { get; init; } = string.Empty;
    public string PipeName { get; init; } = string.Empty;
    public double OuterDiameterMm { get; init; }
    public double WallThicknessMm { get; init; }
    public double EmptyKgPerM { get; init; }
    public double FilledKgPerM { get; init; }
    public double InsulatedKgPerM { get; init; }

    public string DisplayName => $"{MaterialName} - {OuterDiameterMm.ToString("0.#", CultureInfo.InvariantCulture)} mm";
}

public static class PipeBallastCatalog
{
    private static readonly Dictionary<string, List<(double OuterDiameterMm, double EmptyKgPerM, double FilledKgPerM)>> SourceTables =
        new(StringComparer.Ordinal)
        {
            ["Dikwandige CV buis"] =
            [
                (17.2, 0.86, 0.983), (21.3, 1.22, 1.421), (26.9, 1.58, 1.946), (33.7, 2.44, 3.021),
                (42.4, 3.14, 4.152), (48.3, 3.61, 4.982), (60.3, 5.10, 7.305), (76.1, 4.71, 8.66),
                (88.9, 6.15, 11.57), (114.3, 8.77, 17.91), (139.7, 12.08, 25.87), (168.3, 16.21, 36.39),
                (219.1, 23.82, 58.49)
            ],
            ["Dunwandige CV buis"] =
            [
                (10.2, 0.34, 0.38), (13.5, 0.52, 0.60), (16.0, 0.63, 0.751), (17.2, 0.68, 0.825),
                (21.3, 0.95, 1.185), (26.9, 1.40, 1.79), (33.7, 1.99, 2.628), (42.4, 2.55, 3.636),
                (48.3, 2.95, 4.408), (60.3, 4.11, 6.442), (76.1, 5.24, 9.12), (88.9, 6.76, 12.103),
                (114.3, 9.83, 18.834)
            ],
            ["Koperen buis"] =
            [
                (10.0, 0.252, 0.302), (12.0, 0.308, 0.387), (15.0, 0.391, 0.524), (18.0, 0.475, 0.676),
                (22.0, 0.587, 0.901), (28.0, 1.11, 1.601), (35.0, 1.41, 2.214), (42.0, 1.70, 2.894),
                (54.0, 2.91, 4.873), (64.0, 3.47, 6.298), (76.1, 4.17, 8.25), (88.9, 4.89, 10.55),
                (108.0, 7.42, 15.75), (133.0, 10.98, 23.65), (159.0, 13.17, 31.56), (219.0, 18.24, 53.87),
                (267.0, 22.29, 75.79)
            ],
            ["Henco buis"] =
            [
                (14.0, 0.111, 0.19), (16.0, 0.129, 0.242), (20.0, 0.175, 0.376), (25.0, 0.274, 0.588),
                (26.0, 0.300, 0.614), (32.0, 0.415, 0.946), (40.0, 0.595, 1.45), (50.0, 0.84, 2.225),
                (63.0, 1.10, 3.39)
            ],
            ["PE SDR11 buis"] =
            [
                (10.0, 0.05, 0.078), (12.0, 0.06, 0.11), (16.0, 0.08, 0.193), (20.0, 0.11, 0.311),
                (25.0, 0.16, 0.487), (32.0, 0.26, 0.791), (40.0, 0.40, 1.235), (50.0, 0.62, 1.927),
                (63.0, 0.99, 3.065), (75.0, 1.4, 4.342), (90.0, 2.0, 6.254), (110.0, 2.98, 9.342),
                (125.0, 3.87, 12.073), (140.0, 4.86, 15.139), (160.0, 6.34, 19.777), (180.0, 8.01, 25.028),
                (200.0, 9.88, 30.901), (225.0, 12.51, 39.1), (250.0, 15.46, 48.273), (280.0, 19.37, 60.557),
                (315.0, 24.52, 76.637)
            ]
        };

    public static List<PipeBallastSelection> GetAllSelections()
    {
        var selections = new List<PipeBallastSelection>();

        AddMaterialSelections(selections, "Dikwandige CV buis", CopperPipe.GetThickWallSizes());
        AddMaterialSelections(selections, "Dunwandige CV buis", CopperPipe.GetThinWallSizes());
        AddMaterialSelections(selections, "Henco buis", CopperPipe.GetHencoSizes());
        AddMaterialSelections(selections, "Koperen buis", CopperPipe.GetStandardSizes());
        AddMaterialSelections(selections, "PE SDR11 buis", CopperPipe.GetPeSDR11Sizes());

        return selections
            .OrderBy(s => s.MaterialName)
            .ThenBy(s => s.OuterDiameterMm)
            .ToList();
    }

    public static List<string> GetMaterialNames()
    {
        return GetAllSelections()
            .Select(selection => selection.MaterialName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name)
            .ToList();
    }

    public static List<PipeBallastSelection> GetSelectionsByMaterial(string materialName)
    {
        return GetAllSelections()
            .Where(selection => string.Equals(selection.MaterialName, materialName, StringComparison.Ordinal))
            .OrderBy(selection => selection.OuterDiameterMm)
            .ToList();
    }

    private static void AddMaterialSelections(List<PipeBallastSelection> target, string materialName, List<CopperPipe> pipes)
    {
        foreach (var pipe in pipes)
        {
            var (emptyKgPerM, filledKgPerM) = ResolveWeights(materialName, pipe.OuterDiameter, pipe.WallThickness);
            double insulatedKgPerM = CalculateInsulatedWeight(filledKgPerM, pipe.OuterDiameter);

            target.Add(new PipeBallastSelection
            {
                MaterialName = materialName,
                PipeName = string.IsNullOrWhiteSpace(pipe.Name) ? pipe.Omschrijving : pipe.Name,
                OuterDiameterMm = pipe.OuterDiameter,
                WallThicknessMm = pipe.WallThickness,
                EmptyKgPerM = emptyKgPerM,
                FilledKgPerM = filledKgPerM,
                InsulatedKgPerM = insulatedKgPerM
            });
        }
    }

    private static (double EmptyKgPerM, double FilledKgPerM) ResolveWeights(string materialName, double outerDiameterMm, double wallThicknessMm)
    {
        if (SourceTables.TryGetValue(materialName, out var rows) && rows.Count > 0)
        {
            var nearest = rows.OrderBy(r => Math.Abs(r.OuterDiameterMm - outerDiameterMm)).First();
            if (Math.Abs(nearest.OuterDiameterMm - outerDiameterMm) <= 0.8)
            {
                return (nearest.EmptyKgPerM, nearest.FilledKgPerM);
            }
        }

        return EstimateWeightsFromGeometry(materialName, outerDiameterMm, wallThicknessMm);
    }

    private static (double EmptyKgPerM, double FilledKgPerM) EstimateWeightsFromGeometry(string materialName, double outerDiameterMm, double wallThicknessMm)
    {
        double density = materialName switch
        {
            "Koperen buis" => 8930.0,
            "Henco buis" => 1450.0,
            "PE SDR11 buis" => 955.0,
            _ => 7850.0
        };

        double outerRadiusM = outerDiameterMm / 2000.0;
        double innerRadiusM = Math.Max(0.0, outerRadiusM - (wallThicknessMm / 1000.0));

        double pipeAreaM2 = Math.PI * ((outerRadiusM * outerRadiusM) - (innerRadiusM * innerRadiusM));
        double innerAreaM2 = Math.PI * innerRadiusM * innerRadiusM;

        double emptyKgPerM = pipeAreaM2 * density;
        double waterKgPerM = innerAreaM2 * 1000.0;

        return (emptyKgPerM, emptyKgPerM + waterKgPerM);
    }

    private static double CalculateInsulatedWeight(double filledKgPerM, double outerDiameterMm)
    {
        const double insulationThicknessM = 0.019; // 19 mm
        const double insulationDensityKgPerM3 = 45.0; // Typical mineral wool / elastomeric average

        double outerRadiusM = outerDiameterMm / 2000.0;
        double insulatedRadiusM = outerRadiusM + insulationThicknessM;
        double insulationAreaM2 = Math.PI * ((insulatedRadiusM * insulatedRadiusM) - (outerRadiusM * outerRadiusM));
        double insulationKgPerM = insulationAreaM2 * insulationDensityKgPerM3;

        return filledKgPerM + insulationKgPerM;
    }
}
