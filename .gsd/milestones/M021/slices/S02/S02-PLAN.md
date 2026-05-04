# S02: CleanupViewModel 统一 + CT.Mvvm 迁移

**Goal:** Unify CleanupViewModel so MainWindow cleanup Tab and standalone CleanupWindow share the same ViewModel (CT.Mvvm), remove all cleanup code from MainWindowViewModel, and maintain both in-place cleanup (CleanupWindow) and output-directory cleanup (Tab) modes.
**Demo:** 清理 Tab 和独立窗口共用 CleanupViewModel（CT.Mvvm），MainWindowVM 无清理代码

## Must-Haves

- CleanupViewModel uses CT.Mvvm (partial class + [ObservableProperty] + [RelayCommand])
- CleanupViewModel has OutputDirectory property with default path (Documents\DocuFiller输出\清理) and supports both in-place and output-dir cleanup modes
- MainWindow cleanup Tab DataContext binds to CleanupVM (via DockPanel scoping like FillVM)
- MainWindowViewModel has zero cleanup-specific fields, properties, commands, or methods
- MainWindowViewModel no longer depends on IDocumentCleanupService
- OpenCleanupCommand (dead code) removed from MainWindowVM
- MainWindow.xaml.cs cleanup drag-drop handlers call CleanupVM methods instead of manipulating collections directly
- CleanupWindow continues to work unchanged (in-place mode, no output directory)
- dotnet build 0 errors, dotnet test 280 passed

## Proof Level

- This slice proves: integration — real XAML DataContext binding compiles and runtime correctly routes cleanup Tab interactions through CleanupViewModel; CleanupWindow standalone dialog still works

## Verification

- CleanupViewModel StartCleanupAsync logs processing results (success/failure/skip counts) via ILogger — same as current behavior. No new observability surfaces needed.

## Tasks

- [ ] **T01: Rewrite CleanupViewModel with CT.Mvvm + output directory support** `est:1h`
  Migrate CleanupViewModel from hand-written ObservableObject to CT.Mvvm.
  - Files: `DocuFiller/ViewModels/CleanupViewModel.cs`
  - Verify: dotnet build DocuFiller.csproj --no-restore → 0 errors 0 warnings

- [ ] **T02: Wire cleanup Tab to CleanupVM + remove cleanup code from MainWindowVM** `est:1.5h`
  Wire MainWindow cleanup Tab to shared CleanupViewModel and remove all cleanup code from MainWindowViewModel.
  - Files: `ViewModels/MainWindowViewModel.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`, `App.xaml.cs`
  - Verify: dotnet build DocuFiller.csproj --no-restore → 0 errors 0 warnings && dotnet test --no-restore --verbosity minimal → 280 passed

## Files Likely Touched

- DocuFiller/ViewModels/CleanupViewModel.cs
- ViewModels/MainWindowViewModel.cs
- MainWindow.xaml
- MainWindow.xaml.cs
- App.xaml.cs
