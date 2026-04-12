using System;
using System.Collections.Generic;

namespace HVACCalculator.Models;

public class CopperPipe
{
    public string Name { get; init; } = "";
    public string Omschrijving { get; init; } = "";
    public double OuterDiameter { get; init; }   // mm
    public double WallThickness { get; init; }   // mm
    public double InnerDiameter => OuterDiameter - 2 * WallThickness; // mm

    // Max volumestroom in m³/h bij opgegeven snelheid (m/s)
    public double MaxFlow(double velocity)
    {
        double r = InnerDiameter / 2000.0;
        return Math.PI * r * r * velocity * 3600.0;
    }

    // Stroomsnelheid in m/s bij opgegeven volumestroom (m³/h)
    public double Velocity(double qv)
    {
        double r = InnerDiameter / 2000.0;
        double area = Math.PI * r * r;
        return area > 0 ? (qv / 3600.0) / area : 0;
    }

    // Max lineair drukverlies (Pa/m) volgens Darcy-Weisbach + Colebrook-White.
    public double MaxLinearPressureLoss(double velocity, double roughnessMm, double density, double kinematicViscosity)
    {
        double d = InnerDiameter / 1000.0; // m
        if (d <= 0 || velocity <= 0 || density <= 0 || kinematicViscosity <= 0) return 0;

        double reynolds = (velocity * d) / kinematicViscosity;
        if (reynolds <= 0) return 0;

        double relativeRoughness = (roughnessMm / 1000.0) / d;
        double lambda = GetDarcyFrictionFactor(reynolds, relativeRoughness);

        // Darcy-Weisbach per meter: dp/L = lambda * (rho * v^2) / (2 * D)
        return lambda * (density * velocity * velocity) / (2.0 * d);
    }

    // Weerstandswaarde in Pa/m volgens R = f * rho * 8Q^2 / (pi^2 * D^5)
    // met Q in m3/s en D in m.
    public double ResistanceFromFlow(double qvM3PerHour, double roughnessMm, double density, double kinematicViscosity)
    {
        double d = InnerDiameter / 1000.0; // m
        if (d <= 0 || qvM3PerHour <= 0 || density <= 0 || kinematicViscosity <= 0) return 0;

        double q = qvM3PerHour / 3600.0; // m3/s
        double v = (4.0 * q) / (Math.PI * d * d);
        double reynolds = (v * d) / kinematicViscosity;
        if (reynolds <= 0) return 0;

        double relativeRoughness = (roughnessMm / 1000.0) / d;
        double f = GetDarcyFrictionFactor(reynolds, relativeRoughness);

        return f * density * (8.0 * q * q) / (Math.PI * Math.PI * Math.Pow(d, 5));
    }

    private static double GetDarcyFrictionFactor(double reynolds, double relativeRoughness)
    {
        if (reynolds < 2300.0)
        {
            return 64.0 / reynolds;
        }

        // Swamee-Jain als startwaarde voor de Colebrook-White iteratie.
        double lambda = 0.25 / Math.Pow(Math.Log10((relativeRoughness / 3.7) + (5.74 / Math.Pow(reynolds, 0.9))), 2);

        for (int i = 0; i < 20; i++)
        {
            double invSqrtLambda = -2.0 * Math.Log10((relativeRoughness / 3.7) + (2.51 / (reynolds * Math.Sqrt(lambda))));
            double updated = 1.0 / (invSqrtLambda * invSqrtLambda);

            if (Math.Abs(updated - lambda) < 1e-8)
            {
                return updated;
            }

            lambda = updated;
        }

        return lambda;
    }

    public static double GetRoughnessMm(string materialName)
    {
        return materialName switch
        {
            "Koperen buis" => 0.0015,
            "Henco buis" => 0.007,
            "PE SDR11 buis" => 0.007,
            "Dunwandige CV buis" => 0.045,
            "Dikwandige CV buis" => 0.15,
            _ => 0.045
        };
    }

    public static double GetWaterDensity(double temperatureC)
    {
        double t = Math.Clamp(temperatureC, 0.0, 100.0);
        return 1000.0 * (1.0 - ((t + 288.9414) / (508929.2 * (t + 68.12963))) * Math.Pow(t - 3.9863, 2));
    }

    public static double GetWaterKinematicViscosity(double temperatureC)
    {
        double t = Math.Clamp(temperatureC, 0.0, 100.0);
        double mu = 2.414e-5 * Math.Pow(10.0, 247.8 / (t + 133.15)); // Pa.s
        double rho = GetWaterDensity(t); // kg/m3
        return mu / rho; // m2/s
    }

    // Grafiekbenadering: basis voor normale gaspijp met materiaalfactor.
    public double GraphReadPressureLoss(double qv, string materialName, double density, double kinematicViscosity)
    {
        double velocity = Velocity(qv);
        if (velocity <= 0) return 0;

        const double normalGasPipeRoughnessMm = 0.045;
        double baseLoss = MaxLinearPressureLoss(velocity, normalGasPipeRoughnessMm, density, kinematicViscosity);
        double factor = GetGraphMultiplier(materialName);
        return baseLoss * factor;
    }

    public static double GetGraphMultiplier(string materialName)
    {
        return materialName switch
        {
            "Koperen buis" => 0.9,
            "Henco buis" => 0.85,
            "PE SDR11 buis" => 0.85,
            "Dunwandige CV buis" => 1.0,
            "Dikwandige CV buis" => 1.0,
            _ => 1.0
        };
    }

    // Tabel volgens screenshot van gebruiker (buitendiameter/wanddikte)
    public static List<CopperPipe> GetStandardSizes() =>
    [
        new() { Name = "12×1",   OuterDiameter = 12.0,  WallThickness = 1.0 },
        new() { Name = "15×1",   OuterDiameter = 15.0,  WallThickness = 1.0 },
        new() { Name = "22×1,1", OuterDiameter = 22.0,  WallThickness = 1.1 },
        new() { Name = "28×1,2", OuterDiameter = 28.0,  WallThickness = 1.2 },
        new() { Name = "35×1,5", OuterDiameter = 35.0,  WallThickness = 1.5 },
        new() { Name = "42×1,5", OuterDiameter = 42.0,  WallThickness = 1.5 },
        new() { Name = "54×1,5", OuterDiameter = 54.0,  WallThickness = 1.5 },
        new() { Name = "64×2",   OuterDiameter = 64.0,  WallThickness = 2.0 },
        new() { Name = "76,1×2", OuterDiameter = 76.1,  WallThickness = 2.0 },
        new() { Name = "88,9×2", OuterDiameter = 88.9,  WallThickness = 2.0 },
        new() { Name = "108×2",  OuterDiameter = 108.0, WallThickness = 2.0 },
    ];

    // Dunwandige CV buis
    public static List<CopperPipe> GetThinWallSizes() =>
    [
        new() { Name = "12×1",     OuterDiameter = 12.0,  WallThickness = 1.0 },
        new() { Name = "15×1,2",   OuterDiameter = 15.0,  WallThickness = 1.2 },
        new() { Name = "22×1,2",   OuterDiameter = 22.0,  WallThickness = 1.2 },
        new() { Name = "28×1,2",   OuterDiameter = 28.0,  WallThickness = 1.2 },
        new() { Name = "35×1,5",   OuterDiameter = 35.0,  WallThickness = 1.5 },
        new() { Name = "42×1,5",   OuterDiameter = 42.0,  WallThickness = 1.5 },
        new() { Name = "54×1,5",   OuterDiameter = 54.0,  WallThickness = 1.5 },
        new() { Name = "66,7×1,5", OuterDiameter = 66.7,  WallThickness = 1.5 },
        new() { Name = "76,1×2",   OuterDiameter = 76.1,  WallThickness = 2.0 },
        new() { Name = "88,9×2",   OuterDiameter = 88.9,  WallThickness = 2.0 },
        new() { Name = "108×2",    OuterDiameter = 108.0, WallThickness = 2.0 },
    ];

    // Dikwandige CV buis (gelaste stalen gasbuis / vlambuis glad)
    public static List<CopperPipe> GetThickWallSizes() =>
    [
        new() { Omschrijving = "Gelaste stalen gasbuis, 3/8\"",  OuterDiameter = 17.2,  WallThickness = 2.35 },
        new() { Omschrijving = "Gelaste stalen gasbuis, 1/2\"",  OuterDiameter = 21.3,  WallThickness = 2.65 },
        new() { Omschrijving = "Gelaste stalen gasbuis, 3/4\"",  OuterDiameter = 26.9,  WallThickness = 2.65 },
        new() { Omschrijving = "Gelaste stalen gasbuis, 1\"",    OuterDiameter = 33.7,  WallThickness = 3.25 },
        new() { Omschrijving = "Gelaste stalen gasbuis, 1.1/4\"",OuterDiameter = 42.4,  WallThickness = 3.25 },
        new() { Omschrijving = "Gelaste stalen gasbuis, 1.1/2\"",OuterDiameter = 48.3,  WallThickness = 3.25 },
        new() { Omschrijving = "Gelaste stalen gasbuis, 2\"",    OuterDiameter = 60.3,  WallThickness = 3.65 },
        new() { Omschrijving = "Vlambuis glad, 76,1 (2.1/2\")",  OuterDiameter = 76.1,  WallThickness = 2.9  },
        new() { Omschrijving = "Vlambuis glad, 88,9 (3\")",      OuterDiameter = 88.9,  WallThickness = 3.2  },
        new() { Omschrijving = "Vlambuis glad, 114,3 (4\")",     OuterDiameter = 114.3, WallThickness = 3.6  },
        new() { Omschrijving = "Vlambuis glad, 139,7",           OuterDiameter = 139.7, WallThickness = 4.0  },
        new() { Omschrijving = "Vlambuis glad, 168,3",           OuterDiameter = 168.3, WallThickness = 4.5  },
        new() { Omschrijving = "Vlambuis glad, 219,1",           OuterDiameter = 219.1, WallThickness = 6.3  },
    ];

    // Henco buis (meerlaags composiet)
    public static List<CopperPipe> GetHencoSizes() =>
    [
        new() { Name = "14×2",  OuterDiameter = 14.0, WallThickness = 2.0 },
        new() { Name = "16×2",  OuterDiameter = 16.0, WallThickness = 2.0 },
        new() { Name = "18×2",  OuterDiameter = 18.0, WallThickness = 2.0 },
        new() { Name = "20×2",  OuterDiameter = 20.0, WallThickness = 2.0 },
        new() { Name = "26×3",  OuterDiameter = 26.0, WallThickness = 3.0 },
        new() { Name = "32×3",  OuterDiameter = 32.0, WallThickness = 3.0 },
        new() { Name = "40×3,5",OuterDiameter = 40.0, WallThickness = 3.5 },
        new() { Name = "50×4",  OuterDiameter = 50.0, WallThickness = 4.0 },
        new() { Name = "63×4,5",OuterDiameter = 63.0, WallThickness = 4.5 },
        new() { Name = "75×5",  OuterDiameter = 75.0, WallThickness = 5.0 },
        new() { Name = "90×7",  OuterDiameter = 90.0, WallThickness = 7.0 },
    ];

    // PE SDR11 buis
    public static List<CopperPipe> GetPeSDR11Sizes() =>
    [
        new() { Omschrijving = "PE80",    OuterDiameter =  20.0, WallThickness =  2.0  },
        new() { Omschrijving = "PE80",    OuterDiameter =  25.0, WallThickness =  2.3  },
        new() { Omschrijving = "PE80",    OuterDiameter =  32.0, WallThickness =  3.0  },
        new() { Omschrijving = "PE80",    OuterDiameter =  40.0, WallThickness =  3.7  },
        new() { Omschrijving = "PE80",    OuterDiameter =  50.0, WallThickness =  4.6  },
        new() { Omschrijving = "PE80/100",OuterDiameter =  63.0, WallThickness =  5.8  },
        new() { Omschrijving = "PE100",   OuterDiameter =  75.0, WallThickness =  6.8  },
        new() { Omschrijving = "PE100",   OuterDiameter =  90.0, WallThickness =  8.2  },
        new() { Omschrijving = "PE100",   OuterDiameter = 110.0, WallThickness = 10.0  },
        new() { Omschrijving = "PE100",   OuterDiameter = 160.0, WallThickness = 14.6  },
        new() { Omschrijving = "PE100",   OuterDiameter = 200.0, WallThickness = 18.2  },
        new() { Omschrijving = "PE100",   OuterDiameter = 250.0, WallThickness = 22.7  },
        new() { Omschrijving = "PE100",   OuterDiameter = 315.0, WallThickness = 28.6  },
        new() { Omschrijving = "PE100",   OuterDiameter = 400.0, WallThickness = 36.3  },
        new() { Omschrijving = "PE100",   OuterDiameter = 500.0, WallThickness = 45.4  },
        new() { Omschrijving = "PE100",   OuterDiameter = 560.0, WallThickness = 50.8  },
        new() { Omschrijving = "PE100",   OuterDiameter = 630.0, WallThickness = 57.2  },
        new() { Omschrijving = "PE100",   OuterDiameter = 710.0, WallThickness = 64.5  },
    ];
}
