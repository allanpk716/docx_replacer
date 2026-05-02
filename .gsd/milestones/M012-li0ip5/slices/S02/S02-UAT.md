# S02: 拖放焦点修复与最终验证 — UAT

**Milestone:** M012-li0ip5
**Written:** 2026-05-02T01:37:27.799Z

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: This slice modifies XAML attributes and adds a code-behind event handler — the fix is structurally verifiable via source inspection and build. Live runtime testing requires manual drag-drop from Explorer which cannot be automated in this environment.

## Preconditions

- dotnet build succeeds (verified: 0 errors)
- MainWindow.xaml has Window element with AllowDrop and PreviewDragOver
- MainWindow.xaml.cs has Window_PreviewDragOver handler

## Smoke Test

1. Open MainWindow.xaml and confirm the root `<Window>` element contains `AllowDrop="True"` and `PreviewDragOver="Window_PreviewDragOver"` attributes
2. **Expected:** Both attributes present on lines 11-12

## Test Cases

### 1. Build verification

1. Run `dotnet build`
2. **Expected:** 0 errors

### 2. AllowDrop target count

1. Count occurrences of `AllowDrop="True"` in MainWindow.xaml
2. **Expected:** Exactly 4 — Window (L11), TemplatePathTextBox (L138), DataPathTextBox (L183), CleanupDropZoneBorder (L303)

### 3. Window-level drag handler exists

1. Search MainWindow.xaml.cs for `Window_PreviewDragOver`
2. **Expected:** Found at line 34, handler calls `Activate()` when `!IsActive`

### 4. Child drag handlers preserved

1. Count Drop=", DragEnter=", DragOver=" events in MainWindow.xaml
2. **Expected:** 7 Drop, 3 DragEnter, 4 DragOver — no handlers lost by Window-level changes

### 5. Event tunneling not blocked

1. Read Window_PreviewDragOver handler in MainWindow.xaml.cs
2. **Expected:** `e.Handled` is set to `false`, allowing child handlers to receive the event

## Edge Cases

### Window already active during drag

1. Window.PreviewDragOver fires when window is already IsActive
2. **Expected:** Activate() is not called (early return), no redundant activation

### Multiple rapid drag-over events

1. Dragging over window fires many PreviewDragOver events
2. **Expected:** Activate() called each time but is idempotent — no performance issue

## Failure Signals

- dotnet build produces errors
- AllowDrop count is not 4
- Window_PreviewDragOver handler missing or broken
- Child drag handlers missing from XAML

## Not Proven By This UAT

- Actual drag-drop from Explorer with unfocused window (requires manual runtime testing)
- Multi-monitor drag scenarios
- UAC-elevated Explorer dragging to non-elevated app

## Notes for Tester

The Window-level PreviewDragOver handler only calls Activate() — it does not consume the event (e.Handled = false). This is intentional: the handler's sole purpose is to bring the window to the foreground so OLE drag-drop messages are routed correctly. All actual file path extraction and processing happens in the child TextBox handlers.
