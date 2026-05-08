# HVAC Calculator v2026.4 Release Notes

## New Features

### Tool 07 – Pipe Ballast Calculator (Leidingballast)
Dynamic calculator for determining pipe ballast weights based on material, diameter, and length. Features include:
- Material and diameter selection (Copper, Steel, Plastic)
- Automatic weight calculations (kg/m and total kg)
- Dynamic row management with keyboard shortcuts (+ to add, - to remove)
- CSV export functionality for data integration
- Auto-population of material from previously entered rows

### Tool 08 – Channel Ballast Calculator (Kanalen Ballast)
New tool for calculating ballast weights for round and rectangular air channels:
- Shape selection (Round / Rectangular)
- Automatic weight calculations based on channel dimensions
- Dynamic height field behavior (auto-copy from size for round channels, user-editable for rectangular)
- Keyboard shortcuts for efficient data entry
- CSV export for external processing
- Comprehensive weight tables derived from industry standards

### Startup Menu Redesign
Enhanced launcher interface:
- Alphabetically ordered tools (numbered 01–08)
- Compact button layout with scroll capability
- Improved visual organization for quick access
- All eight tools now accessible from the main startup window

## Improvements

### Tool 05 – Warm Tapwater Recirculation (WarmTapwaterCirculatie)
- **Stability fix**: Resolved initialization crash issues
- **Visual warning**: Velocity values exceeding 0.7 m/s now highlighted in red
- **Auto-calculate**: Removed manual "Bereken" button; calculations now automatic
- **Enhanced keyboard flow**: Improved Tab navigation and data entry experience

### Centralized Version Management
- Consistent version display across all window titles and information dialogs
- Single source of truth for application versioning (App.xaml)
- Ensures version consistency in all user-facing elements

### Documentation
- Updated README with feature descriptions and tool inventory
- English version now complete with current feature set

## Release Artifacts
- **Binary**: `HVAC-Calculator-v2026.4-win-x64.zip` (self-contained Windows executable)

## Technical Details
- **Platform**: Windows 10/11, .NET 8.0
- **Architecture**: WPF desktop application
- **Export Format**: CSV with proper data escaping

---

**Release Date**: May 8, 2026  
**Version**: 2026.4
