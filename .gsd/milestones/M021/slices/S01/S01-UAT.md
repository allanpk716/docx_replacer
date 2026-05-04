# S01: FillViewModel + UpdateStatusViewModel 拆分 — UAT

**Milestone:** M021
**Written:** 2026-05-04T10:28:53.130Z

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: This is a refactoring slice with no new user-facing features. Correctness is verified through compilation (XAML binding resolution), existing test suite (280 tests), and structural metrics (line count targets). Runtime behavior is preserved — no new UI paths to test manually.

## Preconditions

- .NET 8 SDK installed
- Project builds successfully with `dotnet build --no-restore`
- NuGet packages restored (restore artifacts present from previous builds)

## Smoke Test

1. Run `dotnet build DocuFiller.csproj --no-restore` — expect 0 errors, 0 warnings
2. Run `dotnet test --no-restore` — expect 280 passed, 0 failed
3. Count `wc -l ViewModels/MainWindowViewModel.cs` — expect ≤400 lines

## Test Cases

### 1. MainWindowViewModel is now a coordinator (≤400 lines)

1. Open `ViewModels/MainWindowViewModel.cs`
2. Verify class contains only: sub-VM properties (FillVM, UpdateStatusVM), cleanup tab logic, window commands, proxy properties
3. Verify no fill-domain properties (TemplatePath, DataPath, etc.) remain
4. Verify no update-domain properties (UpdateStatusMessage, etc.) remain
5. **Expected:** File is ≤400 lines, contains only coordination logic

### 2. FillViewModel contains all keyword-replacement logic

1. Open `ViewModels/FillViewModel.cs`
2. Verify class is `partial class` inheriting `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`
3. Verify presence of `[ObservableProperty]` fields for TemplatePath, DataPath, OutputDirectory, etc.
4. Verify presence of `[RelayCommand]` for BrowseTemplate, BrowseData, StartProcess, CancelProcess, etc.
5. Verify 6 constructor-injected services
6. **Expected:** All fill-domain logic self-contained in FillViewModel

### 3. UpdateStatusViewModel contains all update logic

1. Open `ViewModels/UpdateStatusViewModel.cs`
2. Verify class is `partial class` inheriting `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`
3. Verify UpdateStatus enum is defined in the file
4. Verify CheckUpdateCommand, UpdateStatusClickCommand, OpenUpdateSettingsCommand exist
5. Verify ExtractHostFromUrl helper method present
6. **Expected:** All update-domain logic self-contained in UpdateStatusViewModel

### 4. XAML bindings route to correct sub-ViewModels

1. Open `MainWindow.xaml`
2. Find the keyword replacement tab's DockPanel — verify `DataContext="{Binding FillVM}"`
3. Find status bar update controls — verify bindings use `UpdateStatusVM.*` paths
4. Find status bar progress message — verify binding uses `FillVM.ProgressMessage`
5. Find Exit button — verify it uses `RelativeSource AncestorType=TabItem` to reach MainWindowVM
6. **Expected:** All bindings correctly target the appropriate sub-ViewModel

### 5. DI registration is correct

1. Open `App.xaml.cs`
2. Verify `services.AddTransient<FillViewModel>()` is present
3. Verify `services.AddTransient<UpdateStatusViewModel>()` is present
4. Verify MainWindowViewModel constructor accepts both FillViewModel and UpdateStatusViewModel
5. **Expected:** All three ViewModels properly registered and wired

### 6. Full test suite passes (regression check)

1. Run `dotnet test --no-restore`
2. **Expected:** 280 tests pass (253 DocuFiller.Tests + 27 E2ERegression), 0 failures

## Edge Cases

### Code-behind access to fill domain

1. Open `MainWindow.xaml.cs`
2. Verify drag-drop handlers reference `viewModel.FillVM.HandleSingleFileDropAsync` and `viewModel.FillVM.HandleFolderDropAsync`
3. Verify OnClosing handler references `viewModel.IsProcessing` and `viewModel.CancelProcessCommand`
4. **Expected:** Code-behind accesses fill domain only through coordinator proxy properties or FillVM sub-VM

### Cleanup tab still functional in coordinator

1. Verify MainWindowViewModel still contains cleanup-related properties and commands
2. Verify cleanup tab XAML bindings still reference MainWindowVM directly (no sub-VM)
3. **Expected:** Cleanup tab bindings unchanged — this is intentional (S02 will extract it)

## Failure Signals

- `dotnet build` reports errors or binding warnings
- `dotnet test` shows any failures
- MainWindowViewModel.cs exceeds 400 lines
- XAML contains bindings to properties that no longer exist on the target ViewModel

## Not Proven By This UAT

- Live GUI runtime verification (actual WPF window rendering and interaction)
- Drag-drop functionality at runtime (code-behind handlers exist but not tested with real drag events)
- Update check against real update server (network-dependent)
- Performance characteristics under load

## Notes for Tester

- The worktree has a known NuGet restore issue (`dotnet build` with restore fails); use `--no-restore` flag
- Cleanup tab code remains in MainWindowViewModel — this is by design (S02 will extract it)
- MainWindowViewModel still uses the project's custom ObservableObject.cs (hand-written INPC), not CT.Mvvm — this is the gradual migration strategy
