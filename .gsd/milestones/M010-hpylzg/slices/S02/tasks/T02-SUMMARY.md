---
id: T02
parent: S02
milestone: M010-hpylzg
key_files:
  - ViewModels/MainWindowViewModel.cs
  - MainWindow.xaml
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-04-29T09:49:27.114Z
blocker_discovered: false
---

# T02: Wire gear button into status bar, add source type suffix to UpdateStatusMessage, register UpdateSettingsWindow in DI

**Wire gear button into status bar, add source type suffix to UpdateStatusMessage, register UpdateSettingsWindow in DI**

## What Happened

Modified MainWindowViewModel.cs to add OpenUpdateSettingsCommand, OpenUpdateSettings() method, and source type suffix logic in UpdateStatusMessage getter. The suffix appends "(GitHub)" or "(内网: host)" based on _updateService.UpdateSourceType when the status is not None or Checking. Added ExtractHostFromUrl helper to parse host from URL strings.

Modified MainWindow.xaml status bar to add a 5th column with a ⚙ gear button between UpdateStatusText (Column 2) and the "检查更新" button (shifted from Column 3 to Column 4). The gear button uses OpenUpdateSettingsCommand, has hover effects via ControlTemplate triggers, and opens UpdateSettingsWindow as a modal dialog. When the dialog returns true (user saved), OnPropertyChanged(nameof(UpdateStatusMessage)) is called to immediately refresh the status bar display.

App.xaml.cs DI registration was already completed by T01 (UpdateSettingsViewModel and UpdateSettingsWindow both registered as Transient), so no changes needed there.

## Verification

dotnet build: 0 errors, 0 CS/MC errors. dotnet test: all 192 tests pass (165 + 27). MainWindow.xaml contains OpenUpdateSettingsCommand binding on gear button at Grid.Column=3. UpdateStatusMessage getter references _updateService.UpdateSourceType and _updateService.EffectiveUpdateUrl. "检查更新" button correctly shifted to Grid.Column=4.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build` | 0 | ✅ pass | 3570ms |
| 2 | `dotnet test --no-build` | 0 | ✅ pass | 15000ms |

## Deviations

Plan Step 3 (DI registration in App.xaml.cs) was already completed by T01, so no additional changes needed there.

## Known Issues

None.

## Files Created/Modified

- `ViewModels/MainWindowViewModel.cs`
- `MainWindow.xaml`
