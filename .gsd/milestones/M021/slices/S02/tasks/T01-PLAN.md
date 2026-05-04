---
estimated_steps: 32
estimated_files: 1
skills_used: []
---

# T01: Rewrite CleanupViewModel with CT.Mvvm + output directory support

Migrate CleanupViewModel from hand-written ObservableObject to CT.Mvvm.

## Steps
1. Change base class from `ObservableObject` to `CommunityToolkit.Mvvm.ComponentModel.ObservableObject` (fully qualified to avoid conflict with project's ObservableObject)
2. Add `partial` modifier to class declaration
3. Replace private fields + properties with `[ObservableProperty] private type _fieldName;`:
   - `_isProcessing` → `[ObservableProperty] private bool _isProcessing;`
   - `_progressStatus` → `[ObservableProperty] private string _progressStatus = "等待处理...";`
   - `_progressPercent` → `[ObservableProperty] private int _progressPercent;`
4. Add new `[ObservableProperty] private string _outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DocuFiller输出", "清理");`
5. Add computed properties: `CanStartCleanup` (FileItems.Count > 0 && !IsProcessing), `CanClearList` (FileItems.Count > 0 && !IsProcessing)
6. Add `partial void OnIsProcessingChanged(bool value)` that calls OnPropertyChanged for CanStartCleanup and CanClearList
7. Add commands via [RelayCommand]:
   - `StartCleanupCommand` → `[RelayCommand(CanExecute = nameof(CanStartCleanup))]` on `StartCleanupAsync`
   - `ClearListCommand` → `[RelayCommand(CanExecute = nameof(CanClearList))]` on `ClearList`
   - `RemoveSelectedCommand` → `[RelayCommand]` on `RemoveSelectedFiles` (stub, logs only)
   - `BrowseOutputDirectoryCommand` → `[RelayCommand]` on `BrowseOutputDirectory` (OpenFolderDialog)
   - `OpenOutputFolderCommand` → `[RelayCommand]` on `OpenOutputFolder` (Process.Start)
8. Modify `StartCleanupAsync` to support dual mode:
   - If `string.IsNullOrWhiteSpace(OutputDirectory)` → call `_cleanupService.CleanupAsync(fileItem)` (1-param, in-place)
   - Otherwise → ensure directory exists, call `_cleanupService.CleanupAsync(fileItem, OutputDirectory)` (3-param, output-dir)
   - Result message adapts to mode: in-place shows just status, output-dir shows output path
9. Keep existing `AddFiles(string[])` and `AddFolder(string)` methods (public, called from code-behind drag-drop handlers)
10. Keep `ProgressChanged` event subscription in constructor
11. Add `using CommunityToolkit.Mvvm.ComponentModel;` and `using CommunityToolkit.Mvvm.Input;`

## Must-Haves
- [ ] Class is `public partial class CleanupViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject`
- [ ] All 4 observable fields use [ObservableProperty] with underscore prefix
- [ ] OutputDirectory property with Documents default path
- [ ] Dual-mode StartCleanupAsync (in-place vs output-dir)
- [ ] All commands via [RelayCommand] attribute
- [ ] AddFiles/AddFolder public methods preserved
- [ ] Existing CleanupWindow XAML bindings (FileItems, ProgressStatus, ProgressPercent, CanStartCleanup) still match generated property names

## Inputs

- `DocuFiller/ViewModels/CleanupViewModel.cs`
- `Services/Interfaces/IDocumentCleanupService.cs`
- `Models/CleanupFileItem.cs`
- `Models/CleanupProgressEventArgs.cs`

## Expected Output

- `DocuFiller/ViewModels/CleanupViewModel.cs`

## Verification

dotnet build DocuFiller.csproj --no-restore → 0 errors 0 warnings
