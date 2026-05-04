---
estimated_steps: 51
estimated_files: 4
skills_used: []
---

# T02: Wire cleanup Tab to CleanupVM + remove cleanup code from MainWindowVM

Wire MainWindow cleanup Tab to shared CleanupViewModel and remove all cleanup code from MainWindowViewModel.

## Steps
1. **MainWindowViewModel.cs** — Remove cleanup code:
   - Remove field: `private readonly IDocumentCleanupService _cleanupService;`
   - Remove fields: `_isCleanupProcessing`, `_cleanupProgressStatus`, `_cleanupProgressPercent`, `_cleanupOutputDirectory`
   - Remove collection: `CleanupFileItems`
   - Remove properties: `IsCleanupProcessing`, `CleanupProgressStatus`, `CleanupProgressPercent`, `CanStartCleanup`, `CleanupOutputDirectory`
   - Remove commands: `OpenCleanupCommand`, `RemoveSelectedCleanupCommand`, `ClearCleanupListCommand`, `StartCleanupCommand`, `CloseCleanupCommand`, `BrowseCleanupOutputCommand`, `OpenCleanupOutputFolderCommand`
   - Remove methods: `OpenCleanup()`, `RemoveSelectedCleanup()`, `ClearCleanupList()`, `StartCleanupAsync()`, `CloseCleanup()`, `BrowseCleanupOutput()`, `OpenCleanupOutputFolder()`
   - Remove `using DocuFiller.Services.Interfaces;` if no other usage
   - Remove IDocumentCleanupService from constructor parameters

2. **MainWindowViewModel.cs** — Add CleanupVM reference:
   - Add field: `private readonly CleanupViewModel _cleanupViewModel;`
   - Add property: `public CleanupViewModel CleanupVM => _cleanupViewModel;`
   - Add CleanupViewModel to constructor parameters (after UpdateStatusViewModel)
   - Remove `_cleanupOutputDirectory` initialization from constructor
   - Add `using DocuFiller.ViewModels;` for CleanupViewModel (if not already there — it's in DocuFiller.ViewModels namespace)
   - Target: MainWindowViewModel ≤ 200 lines (from current 390, removing ~200 lines of cleanup code)

3. **MainWindow.xaml** — Update cleanup TabItem (around line 368):
   - Change `<DockPanel>` inside cleanup TabItem to `<DockPanel DataContext="{Binding CleanupVM}">`
   - Update binding `CleanupOutputDirectory` → `OutputDirectory`
   - Update binding `CleanupFileItems` → `FileItems`
   - Update binding `CleanupProgressStatus` → `ProgressStatus`
   - Update binding `CleanupProgressPercent` → `ProgressPercent`
   - Update command `BrowseCleanupOutputCommand` → `BrowseOutputDirectoryCommand`
   - Update command `OpenCleanupOutputFolderCommand` → `OpenOutputFolderCommand`
   - Update command `RemoveSelectedCleanupCommand` → `RemoveSelectedCommand`
   - Update command `ClearCleanupListCommand` → `ClearListCommand`
   - Update command `StartCleanupCommand` → `StartCleanupCommand` (same name in new VM)
   - Remove or repurpose `CloseCleanupCommand` binding on "退出" button — change to `{Binding ExitCommand, RelativeSource={RelativeSource AncestorType=Window}}` or simply remove the button
   - Remove the "退出" button from cleanup tab footer (it was a no-op stub anyway)

4. **MainWindow.xaml.cs** — Update cleanup drag-drop handlers:
   - In `CleanupDropZoneBorder_Drop`: replace `DataContext is MainWindowViewModel viewModel` logic with getting CleanupVM reference
   - Get CleanupVM via: `((MainWindowViewModel)DataContext).CleanupVM` or direct cast
   - Replace `AddCleanupFile(viewModel, path, ...)` and `AddCleanupFolder(viewModel, path)` calls with `cleanupVM.AddFiles(new[] { path })` and `cleanupVM.AddFolder(path)`
   - Remove helper methods `AddCleanupFile` and `AddCleanupFolder` entirely
   - Keep DragEnter/DragLeave/DragOver handlers unchanged (they only modify Border visual state)
   - Remove `viewModel.OnPropertyChanged(nameof(viewModel.CanStartCleanup))` — CleanupVM handles this internally

5. **App.xaml.cs** — Update DI registration:
   - MainWindowViewModel constructor now takes CleanupViewModel — ensure CleanupViewModel is registered before MainWindowViewModel (it already is as Transient)
   - Remove any now-unused cleanup DI entries if applicable (nothing to remove — all services still needed)

## Must-Haves
- [ ] MainWindowViewModel has no cleanup fields, properties, commands, or methods
- [ ] MainWindowViewModel constructor no longer takes IDocumentCleanupService
- [ ] CleanupVM property exposed on MainWindowViewModel for XAML binding
- [ ] Cleanup Tab DockPanel has DataContext="{Binding CleanupVM}"
- [ ] All cleanup Tab bindings updated to match new CleanupViewModel property/command names
- [ ] MainWindow.xaml.cs drag-drop handlers call CleanupVM.AddFiles/AddFolder
- [ ] AddCleanupFile/AddCleanupFolder helper methods removed
- [ ] dotnet build 0 errors
- [ ] dotnet test 280 passed

## Inputs

- `ViewModels/MainWindowViewModel.cs`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- `App.xaml.cs`
- `DocuFiller/ViewModels/CleanupViewModel.cs`

## Expected Output

- `ViewModels/MainWindowViewModel.cs`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- `App.xaml.cs`

## Verification

dotnet build DocuFiller.csproj --no-restore → 0 errors 0 warnings && dotnet test --no-restore --verbosity minimal → 280 passed
