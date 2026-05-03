---
id: T01
parent: S01
milestone: M016
key_files:
  - ViewModels/MainWindowViewModel.cs
  - MainWindow.xaml
  - MainWindow.xaml.cs
key_decisions:
  - Used WindowChrome with WindowStyle=None for custom title bar — preserves system window behaviors (resize, Aero Snap) while allowing custom buttons
  - Pin icon uses emoji (📌/📍) instead of icon resources for simplicity — no resource file changes needed
  - IsTopmost bound via code-behind PropertyChanged subscription rather than XAML binding because Window.Topmost is not bindable in XAML without a wrapper
duration: 
verification_result: passed
completed_at: 2026-05-03T02:23:56.022Z
blocker_discovered: false
---

# T01: Add WindowChrome custom title bar with pin button for topmost toggle and drag-drop hint text in keyword replacement tab

**Add WindowChrome custom title bar with pin button for topmost toggle and drag-drop hint text in keyword replacement tab**

## What Happened

Implemented all 6 steps from the task plan:

1. **MainWindowViewModel**: Added `_isTopmost` backing field, `IsTopmost` property with `SetProperty` change notification, `ToggleTopmostCommand` declaration, and `ToggleTopmost()` method that flips the boolean. Registered the command in the constructor alongside existing commands.

2. **MainWindow.xaml**: Set `WindowStyle="None"` and added `WindowChrome` with `CaptionHeight=32`, `ResizeBorderThickness=4`, `GlassFrameThickness=0`. Added a custom title bar as a `DockPanel.Dock="Top"` Border containing: app icon (📄) + title text, a pin toggle button (bound to `ToggleTopmostCommand`), a minimize button (`─`), and a close button (`✕` with red hover). All buttons use `WindowChrome.IsHitTestVisibleInChrome="True"`.

3. **Pin button visual feedback**: The code-behind subscribes to `ViewModel.PropertyChanged` for `IsTopmost` changes. When toggled, it updates `Window.Topmost` directly, changes the pin icon between 📌 (active) and 📍 (inactive) with opacity change, and switches the tooltip between "取消置顶" and "置顶窗口".

4. **Drag-drop hints**: Added two `TextBlock` hints (11px, #AAAAAA) below the template and data TextBoxes: "提示：可将 .docx 文件或文件夹拖放到上方文本框" and "提示：可将 Excel 文件拖放到上方文本框". Expanded the Grid row definitions from 8 to 10 rows and adjusted all row indices accordingly.

5. **Build verification**: `dotnet build` completed with 0 errors, 95 pre-existing warnings (all nullable reference warnings from test projects).

6. **Drag-drop compatibility**: WindowChrome preserves system window behaviors (resize, Aero Snap). The existing `AllowDrop=True` and `PreviewDragOver` handler remain intact. `WindowChrome.IsHitTestVisibleInChrome="True"` on the title bar border ensures drag events pass through to the content area.

## Verification

dotnet build completed with 0 errors. All changes are in XAML layout and ViewModel property/command wiring — no logic changes to existing drag-drop handlers, processing pipeline, or other features.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build` | 0 | ✅ pass | 8750ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `ViewModels/MainWindowViewModel.cs`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
