# S01: 窗口置顶按钮 + 拖放提示 — UAT

**Milestone:** M016
**Written:** 2026-05-03T02:27:28.416Z

# S01: 窗口置顶按钮 + 拖放提示 — UAT

**Milestone:** M016
**Written:** 2026-05-03

## UAT Type

- UAT mode: human-experience
- Why this mode is sufficient: This slice delivers only UI changes (title bar button and hint text) with no backend/service logic. Visual inspection and manual interaction are the appropriate verification method.

## Preconditions

- Application built and running (GUI mode, no CLI args)
- No special data or configuration required

## Smoke Test

Launch the application. Verify:
1. The title bar shows a custom layout with app icon, title text, pin button (📌), minimize button (─), and close button (✕)
2. The window is resizable and supports Aero Snap (drag to screen edges)

## Test Cases

### 1. Pin button — toggle topmost off → on

1. Launch the application
2. Observe the pin button shows 📍 (lighter opacity) with tooltip "置顶窗口"
3. Click the pin button
4. **Expected:** Pin icon changes to 📌 (full opacity), tooltip changes to "取消置顶", window stays on top of other windows when clicking away

### 2. Pin button — toggle topmost on → off

1. With the window pinned (📌 visible)
2. Click the pin button again
3. **Expected:** Pin icon changes back to 📍 (lighter opacity), tooltip changes to "置顶窗口", window no longer stays on top

### 3. Drag-drop hint — template TextBox

1. Navigate to the 关键词替换 tab (first tab)
2. Look below the 模板文件 TextBox
3. **Expected:** Gray text reads "提示：可将 .docx 文件或文件夹拖放到上方文本框" in ~11px font

### 4. Drag-drop hint — data TextBox

1. In the 关键词替换 tab, look below the 数据文件 TextBox
2. **Expected:** Gray text reads "提示：可将 Excel 文件拖放到上方文本框" in ~11px font

### 5. Window resize still works

1. Drag any window edge or corner
2. **Expected:** Window resizes normally (WindowChrome ResizeBorderThickness=4 preserves this)

### 6. Minimize and close buttons work

1. Click the minimize button (─) in the custom title bar
2. **Expected:** Window minimizes to taskbar
3. Restore the window
4. Click the close button (✕)
5. **Expected:** Window closes

### 7. Existing drag-drop still functional

1. Drag a .docx file onto the 模板文件 TextBox
2. **Expected:** TextBox text updates with the file path
3. Drag an .xlsx file onto the 数据文件 TextBox
4. **Expected:** TextBox text updates with the file path

## Edge Cases

### Rapid pin toggling
1. Click the pin button rapidly 5+ times
2. **Expected:** Final state is consistent with the last click; no crashes or visual glitches

### Pin state persistence across tab switches
1. Pin the window (📌)
2. Switch to 审核清理 tab
3. Switch back to 关键词替换 tab
4. **Expected:** Pin state is still active (📌), window remains on top

## Failure Signals

- Pin button not visible in title bar
- Clicking pin button has no effect on window z-order
- Drag-drop hints not visible below TextBoxes
- Window cannot be resized or loses Aero Snap
- Existing drag-drop file path setting stops working

## Not Proven By This UAT

- Pin state persistence across application restarts (not implemented — resets each launch)
- Behavior on multi-monitor setups with different DPIs
- Compatibility with accessibility tools (screen readers)

## Notes for Tester

- The custom title bar replaces the system default — verify that minimize/maximize/close all work through the custom buttons
- The 2 pre-existing test failures in UpdateSettingsViewModelTests are unrelated to this slice
- The close button has a red hover effect (Background="#FFE81123") — verify it appears on mouse hover
- No maximize button was added (only minimize and close), which is intentional
