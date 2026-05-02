---
id: T01
parent: S02
milestone: M012-li0ip5
key_files:
  - MainWindow.xaml
  - MainWindow.xaml.cs
key_decisions:
  - Set e.Handled = false in PreviewDragOver so child handlers still receive the tunneling event — the handler only activates the window, it does not consume the event.
duration: 
verification_result: passed
completed_at: 2026-05-02T01:35:15.218Z
blocker_discovered: false
---

# T01: Add Window-level AllowDrop=True and PreviewDragOver activation handler to fix drag-drop when unfocused

**Add Window-level AllowDrop=True and PreviewDragOver activation handler to fix drag-drop when unfocused**

## What Happened

Added two changes to fix drag-drop failure when the window is not focused (R054):

1. **MainWindow.xaml**: Added `AllowDrop="True"` and `PreviewDragOver="Window_PreviewDragOver"` attributes to the root Window element, making it an OLE drag-drop target.

2. **MainWindow.xaml.cs**: Added `Window_PreviewDragOver` handler that checks `IsActive` and calls `Activate()` when the window is not in the foreground, with `_logger.LogInformation("Window activated for drag-drop")` for observability. Sets `e.Handled = false` to allow the event to tunnel down to child control handlers.

This follows the WPF tunneling event model: the Window-level PreviewDragOver fires before child element handlers, giving the window a chance to activate itself and receive OLE drag-drop messages.

## Verification

Verified with `dotnet build` — 0 errors, build succeeded. Confirmed `AllowDrop="True"` present on Window element (1 of 4 total AllowDrop attributes in file). Confirmed `Window_PreviewDragOver` handler exists in code-behind.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build 2>&1 | tail -5` | 0 | ✅ pass | 7590ms |
| 2 | `grep -c 'AllowDrop="True"' MainWindow.xaml` | 0 | ✅ pass | 50ms |
| 3 | `head -15 MainWindow.xaml | grep 'AllowDrop'` | 0 | ✅ pass | 50ms |
| 4 | `grep 'Window_PreviewDragOver' MainWindow.xaml.cs` | 0 | ✅ pass | 50ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `MainWindow.xaml`
- `MainWindow.xaml.cs`
