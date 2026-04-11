# HVAC Calculator

A comprehensive Windows desktop application for HVAC (Heating, Ventilation, and Air Conditioning) system design and calculations. Built with WPF and C#, this tool streamlines the calculation of heating systems, tap water distribution, and air duct dimensioning.

## Features

### Multi-Module Calculator
- **CV/GKW Calculator (Scherm 2)** - Central heating and radiator distribution pipe sizing with velocity calculations
- **Tap Water Calculator (Scherm 3)** - Hot and cold water supply line dimensioning with flow rate analysis
- **Air Duct Calculator** - HVAC ductwork sizing and velocity optimization
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
2. Select startup screen options
3. Open desired calculator (CV/GKW, Tap Water, or Air Duct)
4. Enter flow rate or required parameters
5. Select preferred pipe material from dropdown
6. Review suggested pipes in the DataGrid
7. Verify velocity is within optimal range (0.8–2.0 m/s)
8. Select appropriate pipe size for your installation

### Example: CV Pipe Sizing
- Enter flow rate (Φ) and volume flow (qv)
- Select "Dunwandige CV buis" (default for heating systems)
- Calculator displays available pipe sizes with calculated velocities
- Green-highlighted recommendations show optimal selections

## Project Structure

```
HVAC Calculator/
├── Models/
│   └── CopperPipe.cs              # Pipe material database with 5 types
├── MainWindow.xaml                # Application startup window
├── StartupWindow.xaml             # Initialization interface
├── CVGKWWindow.xaml/cs            # Central heating calculator
├── TapwaterWindow.xaml/cs         # Hot/cold water calculator
├── AirDuctWindow.xaml/cs          # Air duct sizing
├── InfoWindow.xaml/cs             # Reference information
└── HVAC Calculator.csproj         # WPF project configuration
```

## Technical Stack

- **Framework:** Windows Presentation Foundation (WPF)
- **Language:** C# (.NET 8.0)
- **UI:** XAML markup with data binding
- **Architecture:** Model-View-CodeBehind pattern
- **Version Control:** Git with semantic versioning

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
