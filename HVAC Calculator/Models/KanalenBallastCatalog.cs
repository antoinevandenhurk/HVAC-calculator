using System;
using System.Collections.Generic;
using System.Linq;

namespace HVACCalculator.Models;

public static class KanalenBallastCatalog
{
    // Ronde luchtkanalen (spiraalbuis) uit de aangeleverde tabel: kg/m.
    private static readonly Dictionary<int, double> RoundWeightsKgPerM = new()
    {
        [80] = 1.1,
        [100] = 1.4,
        [125] = 1.8,
        [160] = 2.3,
        [200] = 2.8,
        [250] = 3.5,
        [315] = 5.3,
        [355] = 6.0,
        [400] = 6.8,
        [450] = 7.6,
        [500] = 8.4,
        [560] = 9.5,
        [630] = 14.2,
        [710] = 16.0,
        [800] = 18.0,
        [900] = 20.3,
        [1000] = 28.1,
        [1120] = 31.5,
        [1250] = 35.2
    };

    public static IReadOnlyList<string> GetChannelShapes()
    {
        return ["Rond", "Rechthoekig"];
    }

    public static double GetWeightPerM(string shape, double sizeMm, double heightMm)
    {
        if (string.Equals(shape, "Rond", StringComparison.Ordinal))
        {
            return GetRoundWeightPerM(sizeMm);
        }

        if (string.Equals(shape, "Rechthoekig", StringComparison.Ordinal))
        {
            return GetRectangularWeightPerM(sizeMm, heightMm);
        }

        return 0;
    }

    private static double GetRoundWeightPerM(double diameterMm)
    {
        if (diameterMm <= 0)
        {
            return 0;
        }

        int key = (int)Math.Round(diameterMm, MidpointRounding.AwayFromZero);
        if (RoundWeightsKgPerM.TryGetValue(key, out double exact))
        {
            return exact;
        }

        // Bij niet-standaard diameter: neem dichtstbijzijnde standaardmaat.
        var nearest = RoundWeightsKgPerM
            .OrderBy(item => Math.Abs(item.Key - diameterMm))
            .First();

        return nearest.Value;
    }

    private static double GetRectangularWeightPerM(double widthMm, double heightMm)
    {
        if (widthMm <= 0 || heightMm <= 0)
        {
            return 0;
        }

        // Herleid uit de meegeleverde tabelwaarden voor niet-geisoleerde luchtkanalen:
        // kg/m ~ 1.30 * (2*(B+H) * s * rho), met rho=7850 kg/m3 en B/H in meter, s in meter.
        // De plaatdikte s volgt de tabelsegmenten in de afbeelding op basis van de grootste zijde.
        double maxSide = Math.Max(widthMm, heightMm);
        double thicknessMm = maxSide switch
        {
            <= 280 => 0.75,
            <= 500 => 0.88,
            <= 1500 => 1.00,
            <= 2000 => 1.13,
            _ => 1.50
        };

        const double steelDensityKgPerM3 = 7850.0;
        const double correctionFactor = 1.30; // voor lock-naad, overlap, verstijvingen

        double perimeterM = 2.0 * ((widthMm / 1000.0) + (heightMm / 1000.0));
        double sheetThicknessM = thicknessMm / 1000.0;

        return correctionFactor * perimeterM * sheetThicknessM * steelDensityKgPerM3;
    }
}
