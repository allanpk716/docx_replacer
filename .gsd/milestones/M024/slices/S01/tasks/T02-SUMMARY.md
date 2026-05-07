---
id: T02
parent: S01
milestone: M024
key_files:
  - MainWindow.xaml
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-05-07T07:41:52.730Z
blocker_discovered: false
---

# T02: Added rotating spinner animation to status bar using Canvas + Ellipse with StrokeDashArray and DataTrigger-driven Storyboard

**Added rotating spinner animation to status bar using Canvas + Ellipse with StrokeDashArray and DataTrigger-driven Storyboard**

## What Happened

Added a spinner rotation animation to the MainWindow.xaml status bar. Changes include:

1. **Window.Resources**: Added a `SpinnerRotateAnimation` Storyboard with a `DoubleAnimation` that rotates from 0° to 360° over 1 second with `RepeatBehavior="Forever"`.

2. **Status bar Column 2 restructure**: Replaced the standalone `UpdateStatusText` TextBlock with a horizontal `StackPanel` containing:
   - A 16x16 `Canvas` spinner element with a dashed-stroke `Ellipse` (`StrokeDashArray="2 2.5"`) and `RotateTransform` centered at (8,8)
   - The existing `UpdateStatusText` TextBlock (preserved all bindings, input bindings, and text decorations)

3. **Animation lifecycle**: The Canvas has a `Style` with a `DataTrigger` bound to `UpdateStatusVM.ShowCheckingAnimation`. When `true`, `BeginStoryboard` starts the rotation; when `false`, `StopStoryboard` halts it. The Canvas visibility is also controlled by `BooleanToVisibilityConverter` on the same property.

4. **Layout preservation**: The StackPanel maintains the same `Margin="15,0,15,0"` as the original TextBlock, so the status bar layout (version | progress | [spinner] status text | ⚙ | check update) is unchanged.

Build verified: `dotnet build DocuFiller.csproj` — 0 warnings, 0 errors.

## Verification

dotnet build DocuFiller.csproj completed with 0 warnings, 0 errors. The XAML compiles cleanly including the Storyboard resource, DataTrigger with BeginStoryboard/StopStoryboard, and the Canvas+Ellipse spinner element.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build DocuFiller.csproj` | 0 | ✅ pass | 4960ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `MainWindow.xaml`
