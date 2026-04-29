# S02: 更新设置弹窗 + 状态栏源类型显示 — UAT

**Milestone:** M010-hpylzg
**Written:** 2026-04-29T09:51:47.582Z

# S02: 更新设置弹窗 + 状态栏源类型显示 — UAT

**Milestone:** M010-hpylzg
**Written:** 2026-04-29

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: This slice is a WPF GUI addition (dialog + status bar button). The core logic (ReloadSource, source type detection) was validated in S01. S02 only wires new UI to existing services. Build + full test suite passing proves all compilation and existing behavior is intact. Runtime dialog verification requires manual GUI launch which is outside automated scope.

## Preconditions

- Application builds successfully with dotnet build
- All 192 existing tests pass
- S01's ReloadSource functionality is in place (IUpdateService.ReloadSource available)

## Smoke Test

- Verify status bar contains a ⚙ gear button between UpdateStatusText and "检查更新" button
- Verify MainWindowViewModel has OpenUpdateSettingsCommand

## Test Cases

### 1. Status bar gear button visibility

1. Open MainWindow.xaml source
2. Verify a Button with Content="⚙" exists at Grid.Column=3 in the status bar Grid
3. Verify the Button's Command is bound to OpenUpdateSettingsCommand
4. **Expected:** Gear button is present with correct binding

### 2. UpdateSettingsWindow displays current source info

1. Construct UpdateSettingsViewModel with a mock IUpdateService returning UpdateSourceType="GitHub"
2. Verify SourceTypeDisplay is "GitHub"
3. Verify UpdateUrl is empty (GitHub mode shows no URL)
4. **Expected:** ViewModel correctly reads source type from IUpdateService

### 3. Save triggers ReloadSource

1. Construct UpdateSettingsViewModel with mock IUpdateService
2. Set UpdateUrl = "http://192.168.1.100:8080", Channel = "beta"
3. Execute SaveCommand
4. **Expected:** IUpdateService.ReloadSource called with ("http://192.168.1.100:8080", "beta")

### 4. UpdateStatusMessage appends source type suffix

1. When _updateService.UpdateSourceType is "GitHub", verify UpdateStatusMessage ends with "(GitHub)"
2. When _updateService.UpdateSourceType is "HTTP" with EffectiveUpdateUrl containing "192.168.1.100:8080", verify suffix is "(内网: 192.168.1.100:8080)"
3. When UpdateCheckState is None or Checking, verify no suffix is appended
4. **Expected:** Correct suffix displayed for each source type

### 5. Existing update flow unaffected (regression)

1. Run dotnet test (all 192 tests)
2. **Expected:** 0 failures — no regression in existing functionality

## Edge Cases

### Empty URL with GitHub source type

1. Set UpdateSourceType to "GitHub"
2. Verify UpdateUrl field shows empty string (not null)
3. **Expected:** User can clear URL to revert to GitHub releases

### Dialog cancel

1. Open UpdateSettingsWindow, make changes, click Cancel
2. **Expected:** DialogResult is false, ReloadSource NOT called

## Failure Signals

- dotnet build returns CS/MC errors → wiring broken
- dotnet test failures → regression in existing functionality
- UpdateSettingsViewModel not registered in DI → runtime crash on dialog open
- Missing Grid.Column definitions → status bar layout broken

## Not Proven By This UAT

- Actual dialog window rendering (requires WPF runtime on Windows)
- Mouse hover effects on gear button (visual only)
- Real-time status bar refresh after dialog save (requires runtime property change notification)
- appsettings.json persistence (handled by S01's ReloadSource)

## Notes for Tester

- This is the final slice in milestone M010 — no downstream slices depend on S02
- The gear button uses a ControlTemplate with hover triggers for visual feedback
- UpdateSettingsViewModel uses CloseCallback pattern (not events) for window control — this is intentional and matches the CleanupWindow pattern
