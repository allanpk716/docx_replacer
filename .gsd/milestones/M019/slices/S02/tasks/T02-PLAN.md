---
estimated_steps: 39
estimated_files: 4
skills_used: []
---

# T02: Set Icon on all windows and replace emoji with Image in MainWindow title bar

Apply the app icon to all WPF windows and replace the 📄 emoji in MainWindow's custom title bar with a proper Image control.

## Context

- **MainWindow.xaml**: Uses `WindowStyle="None"` with custom WindowChrome title bar. The emoji `📄` at line 37 needs replacement with an `<Image>` control. Despite `WindowStyle="None"`, setting `Window.Icon` still affects the taskbar icon.
- **CleanupWindow.xaml**: Standard Window with default WindowStyle. Needs `Icon` attribute.
- **DownloadProgressWindow.xaml**: Standard Window. Needs `Icon` attribute.
- **UpdateSettingsWindow.xaml**: Standard Window. Needs `Icon` attribute.

The icon is embedded as a WPF Resource (from `Resources/app.ico` in csproj `<Resource Include="Resources\**" />`), accessible via `pack://application:,,,/Resources/app.ico`.

## Steps

1. **MainWindow.xaml**: Set `Icon` attribute on the Window element:
   ```xml
   Icon="pack://application:,,,/Resources/app.ico"
   ```
   This makes the taskbar show the icon even with WindowStyle=None.

2. **MainWindow.xaml**: Replace the emoji TextBlock at line 37:
   Before: `<TextBlock Text="📄" FontSize="14" .../>`
   After: `<Image Source="pack://application:,,,/Resources/app.ico" Width="16" Height="16" VerticalAlignment="Center" Margin="0,0,6,0" RenderOptions.BitmapScalingMode="HighQuality"/>`

3. **CleanupWindow.xaml**: Add `Icon="pack://application:,,,/Resources/app.ico"` to the Window element.

4. **DownloadProgressWindow.xaml**: Add `Icon="pack://application:,,,/Resources/app.ico"` to the Window element.

5. **UpdateSettingsWindow.xaml**: Add `Icon="pack://application:,,,/Resources/app.ico"` to the Window element.

6. Run `dotnet build` to verify no errors.
7. Run `dotnet test` to verify all existing tests still pass.

## Must-Haves

- [ ] MainWindow.xaml has `Icon` attribute set to pack URI for app.ico
- [ ] MainWindow.xaml title bar uses `<Image>` instead of emoji TextBlock
- [ ] CleanupWindow.xaml has `Icon` attribute set
- [ ] DownloadProgressWindow.xaml has `Icon` attribute set
- [ ] UpdateSettingsWindow.xaml has `Icon` attribute set
- [ ] `dotnet build` succeeds
- [ ] `dotnet test` passes

## Verification

- `powershell -Command "Select-String -Path MainWindow.xaml -Pattern 'pack://application:,,,/Resources/app.ico'"` returns at least 1 match
- `powershell -Command "Select-String -Path DocuFiller/Views/CleanupWindow.xaml -Pattern 'Icon='"` returns match
- `powershell -Command "Select-String -Path DocuFiller/Views/DownloadProgressWindow.xaml -Pattern 'Icon='"` returns match
- `powershell -Command "Select-String -Path DocuFiller/Views/UpdateSettingsWindow.xaml -Pattern 'Icon='"` returns match
- `powershell -Command "Select-String -Path MainWindow.xaml -Pattern '📄'"` returns NO matches (emoji removed)
- `dotnet build` exits with code 0
- `dotnet test` exits with code 0

## Observability Impact

None — purely visual/UI change.

## Inputs

- `Resources/app.ico`
- `MainWindow.xaml`
- `DocuFiller/Views/CleanupWindow.xaml`
- `DocuFiller/Views/DownloadProgressWindow.xaml`
- `DocuFiller/Views/UpdateSettingsWindow.xaml`

## Expected Output

- `MainWindow.xaml`
- `DocuFiller/Views/CleanupWindow.xaml`
- `DocuFiller/Views/DownloadProgressWindow.xaml`
- `DocuFiller/Views/UpdateSettingsWindow.xaml`

## Verification

dotnet build && dotnet test
