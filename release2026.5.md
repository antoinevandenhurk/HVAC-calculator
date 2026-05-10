# HVAC Calculator v2026.5 Release Notes

## New Features

### Tool 09 - Warmteverlies/afgifte vuistregel
New calculator for fast room-level heat-loss and floor output estimation:
- Building insulation level selector with predefined W/m2 factors
- Dynamic row table with keyboard shortcuts (+ to add, - to remove)
- Per-room inputs for L, B, Ta, Tr, Tv, spacing, pipe diameter, screed thickness and insulation thickness
- Floor covering selector (Steen, Tapijt, Linoleum, Parket)
- Heating and cooling output per m2 and per room total (Wa/Wt and Ka/Kt)
- Condensation risk check using relative humidity and safety margin
- CSV export support for external reporting

## Improvements

### EN 1264-2 calculation model in Tool 09
- Replaced empirical output factors with EN 1264-2 based equivalent screed thickness approach
- Separate surface resistances for heating and cooling are applied
- Results now align with the WTH reference use case discussed during validation

### Tool 09 interface compacting
- Data grid columns and headers were tightened
- Bottom summary panel was compacted with narrower fields and shorter labels
- Formula description labels now wrap to avoid forcing a wide window
- Removed the dedicated Controle column from the table

### Export parity
- Tool 09 export behavior now matches the ballast tools style (CSV with escaped values and UTF-8 BOM)

## Documentation
- Updated README with v2026.5 history and Tool 09 coverage
- Updated startup screenshot caption text to nine available tools

## Release Artifacts
- Binary: HVAC-Calculator-v2026.5-win-x64.zip (self-contained Windows executable)

## Technical Details
- Platform: Windows 10/11, .NET 8.0
- Architecture: WPF desktop application
- Export Format: CSV with proper data escaping

---

Release Date: May 10, 2026
Version: 2026.5
