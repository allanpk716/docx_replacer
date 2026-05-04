---
id: T01
parent: S04
milestone: M020
key_files:
  - ViewModels/DownloadProgressViewModel.cs
  - ViewModels/UpdateSettingsViewModel.cs
  - DocuFiller.csproj
  - Tests/DocuFiller.Tests.csproj
key_decisions:
  - Used fully-qualified CommunityToolkit.Mvvm.ComponentModel.ObservableObject as base class to avoid naming conflict with project's custom ObservableObject class
  - Replaced CommandManager.InvalidateRequerySuggested() with CancelCommand.NotifyCanExecuteChanged() via OnIsDownloadingChanged partial method
  - Added CT.Mvvm 8.4.0 NuGet to test project because it links VM source files directly via Compile Include
duration: 
verification_result: passed
completed_at: 2026-05-04T01:54:37.866Z
blocker_discovered: false
---

# T01: Migrate DownloadProgressViewModel and UpdateSettingsViewModel from hand-written INPC to CommunityToolkit.Mvvm source generators

**Migrate DownloadProgressViewModel and UpdateSettingsViewModel from hand-written INPC to CommunityToolkit.Mvvm source generators**

## What Happened

Migrated two ViewModels from hand-written INotifyPropertyChanged boilerplate to CommunityToolkit.Mvvm 8.4 source generators:

**DownloadProgressViewModel** (304→188 lines):
- Replaced `INotifyPropertyChanged` implementation with inheritance from `CommunityToolkit.Mvvm.ComponentModel.ObservableObject` (fully qualified to avoid naming conflict with project's custom ObservableObject)
- Converted 7 bindable properties to `[ObservableProperty]` fields with underscore prefix convention (`_progressPercent` → generates `ProgressPercent`)
- Replaced manual `CancelCommand` (custom RelayCommand) with `[RelayCommand(CanExecute = nameof(CanCancel))]` source-generated command
- Preserved dispatcher wrapper pattern via `partial void OnIsDownloadingChanged()` calling `CancelCommand.NotifyCanExecuteChanged()` — replacing the old `CommandManager.InvalidateRequerySuggested()` approach
- Removed entire `#region INotifyPropertyChanged` block (SetProperty, OnPropertyChanged, PropertyChanged event)

**UpdateSettingsViewModel** (167→101 lines):
- Same inheritance pattern using CT.Mvvm's ObservableObject
- Converted 2 bindable properties (`UpdateUrl`, `Channel`) to `[ObservableProperty]` fields
- Replaced manual `SaveCommand`/`CancelCommand` with `[RelayCommand]` on `Save()`/`Cancel()` methods
- Fixed MVVMTK0034 warnings by using generated property names instead of direct field references in constructor and Save method
- Removed `#region INotifyPropertyChanged` block

**Infrastructure changes:**
- Added CommunityToolkit.Mvvm 8.4.0 NuGet to both DocuFiller.csproj and Tests/DocuFiller.Tests.csproj (test project links VM source files via `<Compile Include>`)

All 256 tests pass (229 main + 27 E2E), zero build errors.

## Verification

1. `dotnet build` — 0 errors, 71 warnings (all pre-existing, no new warnings from migration)
2. `dotnet test --filter "DownloadProgressViewModelTests|UpdateSettingsViewModelTests"` — 49/49 passed
3. `dotnet test` (full suite) — 256/256 passed (229 + 27 E2E)
4. `grep -c 'ObservableProperty'` — DownloadProgressViewModel: 8 (7 fields + using), UpdateSettingsViewModel: 2
5. `grep -c 'region INotifyPropertyChanged'` — 0 in both files (boilerplate fully removed)
6. `grep 'partial class'` — confirmed both VMs are partial classes
7. `grep -c '_dispatcherInvoke'` — 6 references preserved in DownloadProgressViewModel (thread safety maintained)

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build` | 0 | ✅ pass | 2640ms |
| 2 | `dotnet test --filter 'DownloadProgressViewModelTests|UpdateSettingsViewModelTests'` | 0 | ✅ pass | 2850ms |
| 3 | `dotnet test (full suite)` | 0 | ✅ pass | 17000ms |
| 4 | `grep -c ObservableProperty (both VMs)` | 0 | ✅ pass | 50ms |
| 5 | `grep -c 'region INotifyPropertyChanged' (both VMs)` | 1 | ✅ pass (0 matches) | 50ms |

## Deviations

1. Task plan suggested using `[ObservableObject]` attribute; changed to inheriting from CT.Mvvm's `ObservableObject` base class to address MVVMTK0033 warning. 2. Task plan did not mention adding CT.Mvvm to the test project — discovered during build that the test project links VM source files via `<Compile Include>` and needed the package reference too.

## Known Issues

None.

## Files Created/Modified

- `ViewModels/DownloadProgressViewModel.cs`
- `ViewModels/UpdateSettingsViewModel.cs`
- `DocuFiller.csproj`
- `Tests/DocuFiller.Tests.csproj`
