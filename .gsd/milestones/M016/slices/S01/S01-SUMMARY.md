---
id: S01
parent: M016
milestone: M016
provides:
  - ["Custom WindowChrome title bar with pin/minimize/close buttons", "IsTopmost ViewModel property and ToggleTopmostCommand", "Drag-drop hint TextBlocks in keyword replacement tab"]
requires:
  []
affects:
  []
key_files:
  - ["ViewModels/MainWindowViewModel.cs", "MainWindow.xaml", "MainWindow.xaml.cs"]
key_decisions:
  - ["WindowChrome with WindowStyle=None preserves system resize/Aero Snap while allowing custom title bar buttons", "Pin icon uses emoji (📌/📍) with opacity toggle instead of icon resource files for simplicity", "IsTopmost bound via code-behind PropertyChanged subscription because Window.Topmost is not XAML-bindable directly"]
patterns_established:
  - ["WindowChrome custom title bar pattern: WindowStyle=None + WindowChrome with IsHitTestVisibleInChrome=True on interactive elements", "Code-behind bridging for non-bindable Window properties via ViewModel PropertyChanged subscription"]
observability_surfaces:
  - none
drill_down_paths:
  - [".gsd/milestones/M016/slices/S01/tasks/T01-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-05-03T02:27:28.416Z
blocker_discovered: false
---

# S01: 窗口置顶按钮 + 拖放提示

**Added custom WindowChrome title bar with pin toggle button for topmost and drag-drop hint text below template/data TextBoxes**

## What Happened

Single task (T01) implemented all slice goals:

1. **ViewModel layer**: Added `IsTopmost` property with change notification and `ToggleTopmostCommand` relay command to `MainWindowViewModel`.

2. **Custom title bar via WindowChrome**: Set `WindowStyle="None"` with `WindowChrome` (CaptionHeight=32, ResizeBorderThickness=4) to replace the default title bar. Built a custom title bar containing: app icon + title text, a pin toggle button (bound to ToggleTopmostCommand), a minimize button, and a close button (red hover). All buttons use `WindowChrome.IsHitTestVisibleInChrome="True"` for click-through.

3. **Pin button visual feedback**: Code-behind subscribes to `ViewModel.PropertyChanged` for `IsTopmost`. On toggle: updates `Window.Topmost`, switches icon between 📌 (active, opacity 1.0) and 📍 (inactive, opacity 0.5), and updates tooltip between "取消置顶" and "置顶窗口".

4. **Drag-drop hints**: Added two TextBlock hints (11px, #AAAAAA) below the template and data TextBoxes in the keyword replacement tab, guiding users that drag-drop is supported.

5. **Compatibility**: WindowChrome preserves system resize/Aero Snap. Existing AllowDrop and PreviewDragOver handlers remain intact.

Build verified with 0 errors. Two pre-existing test failures in `UpdateSettingsViewModelTests` are unrelated to this slice (that file was not modified).

## Verification

dotnet build: 0 errors, 0 warnings. 220/222 tests pass; 2 failures in UpdateSettingsViewModelTests (pre-existing, unrelated to this slice — neither UpdateSettingsViewModel nor its tests were modified). XAML structure verified: WindowChrome present, pin button binds to ToggleTopmostCommand, drag-drop TextBlocks in correct Grid rows below template/data TextBoxes.

## Requirements Advanced

None.

## Requirements Validated

- R057 — Pin button in custom title bar toggles Window.Topmost with visual state feedback (📌/📍 + opacity + tooltip)
- R058 — Two TextBlock hints (11px, #AAAAAA) added below template and data TextBoxes in keyword replacement tab

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

None.
