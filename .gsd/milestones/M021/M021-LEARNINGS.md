---
phase: M021
phase_name: 第二轮重构 — ViewModel 拆分 + 拖放 Behavior + R028 自动更新 + 文档同步
project: DocuFiller
generated: "2026-05-04T20:00:00.000Z"
counts:
  decisions: 6
  lessons: 4
  patterns: 5
  surprises: 1
missing_artifacts: []
---

### Decisions

- **DockPanel DataContext scoping over binding prefix**: Used `DockPanel DataContext={Binding FillVM}` to scope bindings per tab instead of prefixing every binding with `FillVM.Property`. This leverages WPF's visual tree inheritance and produces cleaner XAML.
  Source: S01-SUMMARY.md/Key decisions

- **Proxy properties on coordinator for code-behind access**: Added `IsProcessing` and `CancelProcessCommand` proxy properties on MainWindowViewModel coordinator so MainWindow.xaml.cs OnClosing can access FillVM state without casting to FillViewModel type.
  Source: S01-SUMMARY.md/Key decisions

- **IServiceProvider injection in sub-ViewModels for window creation**: Used constructor-injected `IServiceProvider` in UpdateStatusViewModel to create windows instead of `((App)Application.Current).ServiceProvider` cast. Cleaner, testable, and avoids framework coupling.
  Source: S01-SUMMARY.md/Key decisions

- **Sub-ViewModels registered as Transient in DI**: FillViewModel and UpdateStatusViewModel both registered as Transient because they hold mutable per-window state. Singleton would cause shared state bugs across multiple windows.
  Source: S01-SUMMARY.md/Key decisions

- **Dual-mode cleanup dispatch on OutputDirectory emptiness**: CleanupViewModel handles both single-file cleanup (OutputDirectory empty → use input file directory) and folder cleanup (OutputDirectory set → use specified directory) via the same command.
  Source: S02-SUMMARY.md/Key decisions

- **Grid + Ellipse overlay for notification badge**: Used a Grid with overlapping Ellipse for the red dot badge on the ⚙ settings button instead of a custom ControlTemplate. Minimal markup, keeps existing button template untouched.
  Source: S05-SUMMARY.md/Key decisions

### Lessons

- **MainWindowViewModel can be reduced from 1623 to 156 lines by extracting 3 sub-ViewModels**: The coordinator pattern scales well for WPF apps with multiple Tab areas. Each sub-VM manages its own state independently. The key insight is that the coordinator only needs proxy properties for cross-cutting concerns (like window closing).
  Source: S01-SUMMARY.md/What Happened

- **AttachedProperty Behaviors eliminate massive code-behind event handlers**: MainWindow.xaml.cs had 13 drag-drop event handlers (~446 lines). FileDragDrop Behavior reduced this to 104 lines total. The `CommandManager.InvalidateRequerySuggested()` workaround is essential for WPF OLE drag-drop CanExecute staleness.
  Source: S03-SUMMARY.md/What Happened

- **WPF TextBox requires Preview tunnel events for drag-drop, Border uses bubbling**: TextBox has built-in drag-drop handling that intercepts at the bubbling stage. Preview (tunnel) events fire first and can set `e.Handled=true` to prevent interception. Non-TextBox elements like Border have no built-in handling, so bubbling events work fine.
  Source: S03-SUMMARY.md/Key decisions

- **Machine-level NuGet configuration corruption can masquerade as code build failures**: `dotnet nuget list source` returning `Value cannot be null. (Parameter 'path1')` affects all projects on the machine, including master branch. Always verify whether build failures are environment-wide before flagging code issues.
  Source: Milestone completion verification

### Patterns

- **Coordinator + sub-ViewModel with DockPanel DataContext scoping**: MainWindowVM holds child VM references as properties. Each Tab content area wraps in `DockPanel DataContext={Binding ChildVM}`. All bindings within the DockPanel automatically resolve against the child VM. No prefix clutter in XAML.
  Source: S01-SUMMARY.md/Patterns established

- **CT.Mvvm extraction pattern for existing ViewModels**: Convert to `partial class`, change base to `CommunityToolkit.Mvvm.ComponentModel.ObservableObject` (fully qualified to avoid collision with custom ObservableObject), replace manual properties with `[ObservableProperty] private type _field;`, replace commands with `[RelayCommand]`, use `partial void OnXxxChanged()` for side effects.
  Source: S01-SUMMARY.md/Patterns established

- **AttachedProperty Behavior for reusable UI event handling**: Static class with `IsEnabled`, `Filter`, and `DropCommand` attached properties. `OnIsEnabledChanged` subscribes to drag-drop events on the target element. Commands decouple behavior from specific ViewModels.
  Source: S03-SUMMARY.md/Patterns established

- **Delayed initialization with Task.Delay + CancellationToken**: `public InitializeAsync()` wraps `Task.Delay(5000, _cts.Token)` before calling the actual logic method. `OperationCanceledException` is silently caught for graceful shutdown. Testable via reflection calling the private method directly.
  Source: S05-SUMMARY.md/Patterns established

- **Computed notification property with change chain**: `HasUpdateAvailable` computed from `UpdateStatus == UpdateStatus.UpdateAvailable`. `OnUpdateStatusChanged()` raises `HasUpdateAvailable` notification. XAML binds directly to the computed property without converters.
  Source: S05-SUMMARY.md/Patterns established

### Surprises

- **S04 found no code changes needed**: The service interface audit (IDocumentProcessor) and cleanup service logging audit (DocumentCleanupService) both confirmed existing code was already correct. This is a positive surprise — the codebase was in better shape than expected for these areas.
  Source: S04-SUMMARY.md/What Happened
