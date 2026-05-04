# S04: S04 — UAT

**Milestone:** M020
**Written:** 2026-05-04T02:01:43.966Z

# UAT: S04 — CT.Mvvm Migration & Documentation

## UAT Type
Contract-level: code compiles, all tests pass, grep-based pattern verification confirms CT.Mvvm adoption and async void elimination. No runtime integration boundary crossed — pure code standardization.

## Preconditions
- .NET 8 SDK installed
- Project restores successfully (`dotnet restore`)

## Test Cases

### TC-01: CT.Mvvm Source Generators Active
1. Open `ViewModels/DownloadProgressViewModel.cs`
2. Verify class declaration: `partial class DownloadProgressViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject`
3. Count `[ObservableProperty]` attributes — expect 7
4. Verify `[RelayCommand(CanExecute = nameof(CanCancel))]` on Cancel method
5. Verify `partial void OnIsDownloadingChanged(bool value)` exists
6. **Expected**: All CT.Mvvm patterns present, no `#region INotifyPropertyChanged`

### TC-02: UpdateSettingsViewModel Migrated
1. Open `ViewModels/UpdateSettingsViewModel.cs`
2. Verify class declaration: `partial class UpdateSettingsViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject`
3. Count `[ObservableProperty]` attributes — expect 2 (UpdateUrl, Channel)
4. Verify `[RelayCommand]` on Save() and Cancel() methods
5. **Expected**: All CT.Mvvm patterns present, no hand-written INPC

### TC-03: No Async Void in ViewModels
1. Search `ViewModels/` and `DocuFiller/ViewModels/` for `async void`
2. **Expected**: 0 matches in any ViewModel file

### TC-04: Code-Behind Async Void Is Acceptable
1. Search `MainWindow.xaml.cs` for `async void` — expect exactly 1 (`TemplatePathTextBox_PreviewDrop`) with try/catch
2. Search `DocuFiller/Views/CleanupWindow.xaml.cs` for `async void` — expect exactly 1 (`OnStartCleanupClick`)
3. **Expected**: Both code-behind handlers have proper error handling

### TC-05: Build and Test
1. Run `dotnet build --no-restore`
2. **Expected**: 0 errors
3. Run `dotnet test --no-build --verbosity minimal`
4. **Expected**: All tests pass (256/256)

### TC-06: CLAUDE.md Documentation Accuracy
1. Open `CLAUDE.md`, verify "ViewModel Architecture" section exists with coordinator pattern description
2. Verify table lists 4 ViewModels with correct base class annotations
3. Verify CT.Mvvm usage conventions documented (ObservableProperty, RelayCommand, partial methods)
4. Verify CLI section includes `update` subcommand
5. Verify error code table includes UPDATE_CHECK_ERROR and UPDATE_DOWNLOAD_ERROR
6. Verify file structure includes `Utils/OpenXmlHelper.cs` (not OpenXmlTableCellHelper)
7. **Expected**: All documentation matches actual codebase

## Not Proven By This UAT
- Runtime behavior of DownloadProgressWindow and UpdateSettingsWindow UI (not exercised)
- Performance impact of source-generated INPC vs hand-written (not benchmarked)
- Migration of remaining ViewModels (MainWindowViewModel, CleanupViewModel) — deferred per gradual migration decision
