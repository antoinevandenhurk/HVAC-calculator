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
