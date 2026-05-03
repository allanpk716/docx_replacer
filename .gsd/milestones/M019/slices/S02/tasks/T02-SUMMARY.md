---
id: T02
parent: S02
milestone: M019
key_files:
  - MainWindow.xaml
  - DocuFiller/Views/CleanupWindow.xaml
  - DocuFiller/Views/DownloadProgressWindow.xaml
  - DocuFiller/Views/UpdateSettingsWindow.xaml
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-05-03T11:51:59.499Z
blocker_discovered: false
---

# T02: Apply app icon to all WPF windows (MainWindow, CleanupWindow, DownloadProgressWindow, UpdateSettingsWindow) and replace emoji with Image control in custom title bar

**Apply app icon to all WPF windows (MainWindow, CleanupWindow, DownloadProgressWindow, UpdateSettingsWindow) and replace emoji with Image control in custom title bar**

## What Happened

Applied the pack URI icon (`pack://application:,,,/Resources/app.ico`) to all four WPF windows:

1. **MainWindow.xaml**: Added `Icon` attribute on the Window element (affects taskbar even with WindowStyle=None), and replaced the 📄 emoji TextBlock with an `<Image>` control (16x16, HighQuality bitmap scaling) in the custom title bar's StackPanel.

2. **CleanupWindow.xaml**: Added `Icon="pack://application:,,,/Resources/app.ico"` to the Window element.

3. **DownloadProgressWindow.xaml**: Added `Icon="pack://application:,,,/Resources/app.ico"` to the Window element.

4. **UpdateSettingsWindow.xaml**: Added `Icon="pack://application:,,,/Resources/app.ico"` to the Window element.

During editing, discovered that MainWindow.xaml had a duplicate closing tag (corrupted tail `</Window>id>\n    </DockPanel>\n</Window>`) which caused build error MC3000. Fixed by removing the duplicate tail. The initial emoji replacement had been applied to the duplicate section and was lost when the corruption was cleaned up, so re-applied the Image control replacement on the actual emoji TextBlock at line 38.

## Verification

Verified with Select-String that all four XAML files contain the Icon attribute with the correct pack URI. Verified the emoji 📄 is no longer present in MainWindow.xaml. Verified the Image control with RenderOptions.BitmapScalingMode="HighQuality" is present. `dotnet build` succeeded with 0 errors, 0 warnings. `dotnet test` passed all 249 tests (222 unit + 27 E2E).

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `Select-String -Path MainWindow.xaml -Pattern 'pack://application:,,,/Resources/app.ico'` | 0 | ✅ pass | 1500ms |
| 2 | `Select-String -Path DocuFiller/Views/CleanupWindow.xaml -Pattern 'Icon='` | 0 | ✅ pass | 1200ms |
| 3 | `Select-String -Path DocuFiller/Views/DownloadProgressWindow.xaml -Pattern 'Icon='` | 0 | ✅ pass | 1200ms |
| 4 | `Select-String -Path DocuFiller/Views/UpdateSettingsWindow.xaml -Pattern 'Icon='` | 0 | ✅ pass | 1200ms |
| 5 | `grep -c 📄 MainWindow.xaml` | 1 | ✅ pass (no matches) | 500ms |
| 6 | `dotnet build` | 0 | ✅ pass | 2310ms |
| 7 | `dotnet test` | 0 | ✅ pass (249/249) | 11000ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `MainWindow.xaml`
- `DocuFiller/Views/CleanupWindow.xaml`
- `DocuFiller/Views/DownloadProgressWindow.xaml`
- `DocuFiller/Views/UpdateSettingsWindow.xaml`
