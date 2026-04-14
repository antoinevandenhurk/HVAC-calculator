# HVAC Calculator

A comprehensive Windows desktop application for HVAC (Heating, Ventilation, and Air Conditioning) system design and calculations. Built with WPF and C#, this tool streamlines the calculation of heating systems, tap water distribution, air duct dimensioning, mixing water systems, and ISSO user modules.

## Screenshots

| Startup menu | Air Duct Calculator (Scherm 1) | User Modules Calculator (Scherm 5) |
|:---:|:---:|:---:|
| [![Startup menu](HVAC%20Calculator/Resources/Screenshot01.png)](HVAC%20Calculator/Resources/Screenshot01.png) | [![Air Duct Calculator](HVAC%20Calculator/Resources/Screenshot02.png)](HVAC%20Calculator/Resources/Screenshot02.png) | [![User Modules Calculator](HVAC%20Calculator/Resources/Screenshot03.png)](HVAC%20Calculator/Resources/Screenshot03.png) |
| Select one of the five calculators | Calculate duct dimensions from flow rate, velocity and cross-section | Enter Φ and temperatures to derive all flow rates; transfer directly to pipe sizer |

## Version History

### v2026.2 (April 2026)
- **Automatic pipe selection on transfer** — opening Venster 2 from the User Modules screen now immediately shows the pipe diameter table without requiring an extra click
- **Cooling mode support** — all flow rate calculations now use the absolute temperature difference |θ2 − θ3|, preventing invalid results when the system operates in cooling mode
- **ISSO modules 1–9** — the User Modules calculator now supports all nine ISSO system modules with dedicated diagrams and module-specific bypass/flow logic
- **Button alignment fix** — the Afsluiten (Close) button in Scherm 5 is now correctly aligned with the other action buttons

### v2026.1 (April 2026)
- Added Mixing Water Calculator (Scherm 4) with T-junction diagram and balanced flow equations
- Added User Modules Calculator (Scherm 5) with ISSO module selection, diagram display, and Φ-driven flow derivation
- Direct transfer from Scherm 5 to CV/GKW Calculator (Scherm 2) via per-flow-rate buttons, pre-filling qv, θ2, θ3 and pipe material

## Features

### Multi-Module Calculator
- **CV/GKW Calculator (Scherm 2)** - Central heating and radiator distribution pipe sizing with velocity calculations
- **Tap Water Calculator (Scherm 3)** - Hot and cold water supply line dimensioning with flow rate analysis
- **Air Duct Calculator (Scherm 1)** - HVAC ductwork sizing and velocity optimization
- **Mixing Water Calculator (Scherm 4)** - Three-stream mixing point balancer; calculates any missing flow rate or temperature using energy and mass balance
- **User Modules Calculator (Scherm 5)** - ISSO standard system modules 1–9 with visual diagrams; enter Φ and all temperatures to automatically derive all flow rates (qv1, qv2, qv5)
- **Information Window** - Reference materials and calculation guidelines

### Pipe Material Database
Select from 5 pipe material types for precise sizing:
- **Koperen buis** (Copper pipe)
- **Dunwandige CV buis** (Thin-wall heating pipe)
- **Dikwandige CV buis** (Thick-wall heating pipe)
- **Henco buis** (Henco composite pipe)
- **PE SDR11 buis** (Polyethylene pipe)

### Smart Pipe Selection
- Real-time velocity filtering (0.8–2.0 m/s optimal range)
- Interactive DataGrid with inner/outer diameter and velocity calculations
- Material-specific pipe size suggestions based on flow rates
- Color-coded recommendations for optimal pipe selection

### User Experience
- Intuitive multi-window interface
- Metric system calculations (mm, m/s, l/s, etc.)
- Real-time calculation updates
- Dutch language UI
- Bottom-aligned sub-window positioning for ergonomic workflow

## Installation

### Requirements
- Windows 10/11
- .NET 8.0 Runtime or later
- Visual Studio 2022 (for development)

### Build from Source
```bash
git clone https://github.com/antoinevandenhurk/HVAC-calculator.git
cd "HVAC Calculator"
dotnet build -c Release
```

### Run Application
```bash
dotnet run
# or
"HVAC Calculator\bin\Release\net8.0-windows\HVAC Calculator.exe"
```

## Usage

### Basic Workflow
1. Launch the application
2. Select a calculator from the startup menu
3. Enter the required parameters for your chosen screen
4. Click **Bereken** to calculate the result
5. Use the **Venster 2** transfer buttons (Scherm 5) to jump directly to the CV/GKW pipe sizer

### Example: CV Pipe Sizing (Scherm 2)
- Enter flow rate (Φ) and volume flow (qv)
- Select "Dunwandige CV buis" (default for heating systems)
- Calculator displays available pipe sizes with calculated velocities
- Green-highlighted recommendations show optimal selections

### Example: Mixing Water (Scherm 4)
- Enter any five of the six fields: qv1, θ1, qv2, θ2, qv3, θ3
- Leave exactly one field empty
- Click **Bereken** — the missing value is derived from the energy and mass balance

### Example: ISSO User Modules (Scherm 5)
1. Select the ISSO module (1–9) from the dropdown — the corresponding diagram is shown on the left
2. Fill in:
   - **Φ** — thermal power of the module (kW)
   - **θ1 – θ3** — system temperatures (°C)
   - **ρw / cw** — fluid constants (defaults: 981 kg/m³ and 4.19 kJ/kg·K)
3. Click **Bereken** — qv1, qv5 and qv2 are calculated automatically
4. Click **Venster 2** next to any flow rate to open the CV/GKW pipe sizer with qv and temperatures pre-filled and the pipe table immediately populated using Dikwandige CV buis

## Project Structure

```
HVAC Calculator/
├── Models/
│   └── CopperPipe.cs                  # Pipe material database with 5 types
├── Resources/
│   ├── isso_mod1.png – isso_mod9.png  # ISSO module diagrams
│   ├── VA.ico                         # Application icon
│   └── ko-fi.png
├── StartupWindow.xaml/cs              # Calculator selection menu
├── AirDuctWindow.xaml/cs              # Scherm 1 – Air duct sizing
├── CVGKWWindow.xaml/cs                # Scherm 2 – Central heating pipe calculator
├── TapwaterWindow.xaml/cs             # Scherm 3 – Tap water pipe calculator
├── MengwaterWindow.xaml/cs            # Scherm 4 – Mixing water calculator
├── GebruikersModulesWindow.xaml/cs    # Scherm 5 – ISSO user modules calculator
├── InfoWindow.xaml/cs                 # Reference information
├── SettingsWindow.xaml/cs             # Application settings
├── AppSettings.cs                     # Persisted user settings
└── HVAC Calculator.csproj             # WPF project configuration
```

## Technical Stack

- **Framework:** Windows Presentation Foundation (WPF)
- **Language:** C# (.NET 8.0)
- **UI:** XAML markup with data binding
- **Architecture:** Model-View-CodeBehind pattern
- **Version Control:** Git with semantic versioning

## Assets & Resources
When adding new images or icons:
1. Place files in `/Resources/`
2. Set **Build Action** to `Resource` in Visual Studio
3. Reference in XAML using `Source="/Resources/filename.png"`

## Versioning

This project follows semantic versioning. Major releases include significant feature additions:

- **v1.0** - Initial air duct calculator
- **v2.0** - Added pipe selection module with 5 material types, bottom-aligned windows, alphabetical material sorting

View commit history: `git log --oneline`

## Future Enhancements

Potential improvements for future versions:
- Export calculations to PDF/Excel
- Load/save project configurations
- Metric ↔ Imperial unit conversion
- Additional pipe materials database expansion
- Pressure drop calculations
- Thermal efficiency analysis

## License

See [LICENSE](LICENSE) file for details.

## Author

Antoine van den Hurk  
GitHub: [@antoinevandenhurk](https://github.com/antoinevandenhurk)

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests to help improve the HVAC Calculator.

## Support

For bug reports and feature requests, please open an issue on the [GitHub repository](https://github.com/antoinevandenhurk/HVAC-calculator/issues).
