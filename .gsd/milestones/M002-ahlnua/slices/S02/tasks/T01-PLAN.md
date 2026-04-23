---
estimated_steps: 1
estimated_files: 1
skills_used: []
---

# T01: Replace three OpenFileDialog folder hacks with OpenFolderDialog

Replace BrowseOutput, BrowseTemplateFolder, and BrowseCleanupOutput methods in MainWindowViewModel.cs with proper OpenFolderDialog usage, matching the pattern already used in ConverterWindowViewModel.BrowseOutput(). Then run full test suite to confirm zero regressions and grep-scan to verify no OpenFileDialog remains for folder selection.

## Inputs

- `ViewModels/MainWindowViewModel.cs`
- `ViewModels/ConverterWindowViewModel.cs`

## Expected Output

- `ViewModels/MainWindowViewModel.cs`

## Verification

dotnet test --no-restore 2>&1 | tail -5
# AND
grep -c "OpenFileDialog" ViewModels/MainWindowViewModel.cs
# AND
grep -c "OpenFolderDialog" ViewModels/MainWindowViewModel.cs

## Observability Impact

Signals added: _logger.LogInformation for each folder selection result, consistent with S01 ILogger baseline
