---
id: M016
title: "窗口置顶开关 + 拖放提示"
status: complete
completed_at: 2026-05-03T02:30:48.330Z
key_decisions:
  - WindowChrome with WindowStyle=None preserves system resize/Aero Snap while allowing custom title bar buttons
  - Pin icon uses emoji (📌/📍) with opacity toggle instead of icon resource files for simplicity
  - IsTopmost bound via code-behind PropertyChanged subscription because Window.Topmost is not XAML-bindable directly
key_files:
  - ViewModels/MainWindowViewModel.cs
  - MainWindow.xaml
  - MainWindow.xaml.cs
lessons_learned:
  - WindowChrome is the lightest approach for custom title bar buttons in WPF — avoids the heavy cost of full chromeless windows while preserving system window behaviors
  - Window.Topmost is not a dependency property and cannot be directly XAML-bound; code-behind PropertyChanged bridging is the standard workaround
  - Emoji icons (📌/📍) are a pragmatic zero-dependency alternative to icon resource files for simple toggle states in internal desktop tools
---

# M016: 窗口置顶开关 + 拖放提示

**Added WindowChrome custom title bar with pin toggle button for window topmost and drag-drop hint TextBlocks below template/data TextBoxes in keyword replacement tab**

## What Happened

## What Happened

This milestone delivered two UI improvements in a single slice:

1. **WindowChrome custom title bar with pin button**: Replaced the default WPF title bar with a WindowChrome-based custom title bar. The custom bar includes a pin emoji button (📌/📍) that toggles `Window.Topmost` via `ToggleTopmostCommand` on the ViewModel. Active state shows the pin at full opacity with "取消置顶" tooltip; inactive state shows a lighter icon with "置顶窗口" tooltip. WindowChrome preserves system resize behavior and Aero Snap. The `IsTopmost` property is synced to `Window.Topmost` via code-behind `PropertyChanged` subscription since `Window.Topmost` is not directly XAML-bindable.

2. **Drag-drop hint text**: Added subtle hint TextBlocks (11px, #AAAAAA) below the template and data TextBoxes in the keyword replacement tab. These read "提示：可将 .docx 文件或文件夹拖放到上方文本框" and "提示：可将 Excel 文件拖放到上方文本框" respectively, addressing user confusion after the M012 compactification removed the dedicated drag-drop Border area.

Three files were modified: `MainWindow.xaml` (custom title bar layout + drag hints), `MainWindow.xaml.cs` (code-behind Topmost sync + title bar drag handling), and `ViewModels/MainWindowViewModel.cs` (IsTopmost property + ToggleTopmostCommand).

## Success Criteria Results

- ✅ **图钉按钮点击切换 Window.Topmost**: S01 implemented ToggleTopmostCommand bound to pin button. IsTopmost property synced to Window.Topmost via code-behind PropertyChanged subscription. Verified through code review — pin button click handler invokes command, ViewModel property flips, code-behind updates Window.Topmost.
- ✅ **关键词替换 tab 有拖放提示文字**: S01 added two TextBlock elements below template and data TextBoxes with 11px #AAAAAA styling. Verified through code review — TextBlocks present in XAML with correct text content.
- ✅ **dotnet build 编译通过**: `dotnet build` completed with 0 warnings, 0 errors.
- ✅ **现有测试不回归**: 222 of 222 tests that passed before M016 still pass. 2 pre-existing failures in UpdateSettingsViewModelTests are unrelated (verified by running same tests against pre-M016 commit 314f24b).

## Definition of Done Results

- ✅ All slices complete: S01 (1 task) — complete
- ✅ All slice summaries exist: `.gsd/milestones/M016/slices/S01/S01-SUMMARY.md` present
- ✅ Cross-slice integration: Single slice milestone, no cross-slice concerns
- ✅ Code changes verified: 3 non-.gsd files modified with meaningful changes
- ✅ Build passes: dotnet build — 0 errors, 0 warnings
- ✅ No test regressions: 222 pass, 2 pre-existing failures unchanged

## Requirement Outcomes

- R057: active → validated — S01 added pin button in WindowChrome title bar. ToggleTopmostCommand flips IsTopmost, code-behind syncs Window.Topmost. Active/inactive visual states with opacity and tooltip. dotnet build passes.
- R058: active → validated — S01 added TextBlock hints (11px, #AAAAAA) below template and data TextBoxes in keyword replacement tab. dotnet build passes.

## Deviations

None.

## Follow-ups

None.
