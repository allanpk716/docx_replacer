# S02: 应用图标创建与应用

**Goal:** 为 DocuFiller 创建专属应用图标（Word 文档批量填充主题），应用到 exe 资源、所有窗口标题栏和任务栏。替换 MainWindow 标题栏中的 emoji 为真实图标。
**Demo:** 主窗口、子窗口、任务栏和 exe 文件都显示 DocuFiller 专属图标，标题栏 emoji 替换为真实图标

## Must-Haves

- ## Success Criteria Met\n\n- ✅ ProgressBar 填充区域随 Value 变化从左向右增长 (S01)\n- ✅ 所有窗口（MainWindow、DownloadProgressWindow、UpdateSettingsWindow、CleanupWindow）显示图标\n- ✅ exe 文件资源中包含图标 (ApplicationIcon in csproj)\n- ✅ dotnet build 无错误

## Proof Level

- This slice proves: **Proof Level**: visual-verification\n- Real runtime required: yes (WPF UI + exe icon visible in Explorer/taskbar)\n- Human/UAT required: yes (visual confirmation of icon appearance)\n- Automated verification: `dotnet build` passes, `dotnet test` passes, grep confirms Icon attributes present

## Integration Closure

- Upstream surfaces consumed: none (independent slice)\n- New wiring introduced: `<ApplicationIcon>` in csproj, `Icon=` on all Window elements, `<Image>` in title bar\n- What remains: nothing — milestone complete after S01 + S02

## Verification

- None — purely visual/UI resource change with no runtime observability implications.

## Tasks

- [x] **T01: Generate app.ico and configure ApplicationIcon in csproj** `est:30m`
  Use a Python/Pillow script to programmatically generate a DocuFiller application icon (256x256 PNG converted to multi-resolution .ico). The icon should represent a Word document with a fill/overlay symbol, using professional colors. Then configure the csproj with `<ApplicationIcon>Resources\app.ico</ApplicationIcon>` and ensure the Resources directory exists with the icon file. Also add the icon as a WPF Resource so it can be referenced from XAML via pack URI.

## Steps

1. Create `Resources/` directory in the project root
2. Write a Python script that uses Pillow to generate a professional app icon:
   - Size: 256x256 (will be saved as multi-resolution .ico with 16, 32, 48, 64, 128, 256)
   - Design: A stylized Word document (blue rectangle with folded corner) with a green checkmark or fill indicator
   - Colors: Professional blue (#2B579A for document body), lighter blue (#4A90D9 for accent), green (#4CAF50 for checkmark)
   - Background: Transparent
3. Run the script to generate `Resources/app.ico`
4. Also save a `Resources/app.png` (256x256) for use in XAML Image controls
5. Edit `DocuFiller.csproj`: Add `<ApplicationIcon>Resources\app.ico</ApplicationIcon>` inside the main `<PropertyGroup>` (after the existing properties like `<Version>`)
6. Run `dotnet build` to verify the icon embeds into the exe without errors
7. Verify the exe has the icon by checking file properties (optional, manual)

## Must-Haves

- [ ] `Resources/app.ico` exists with multi-resolution icon (16-256px)
- [ ] `Resources/app.png` exists (256x256 PNG with transparency)
- [ ] `DocuFiller.csproj` contains `<ApplicationIcon>Resources\app.ico</ApplicationIcon>` in PropertyGroup
- [ ] `dotnet build` succeeds with 0 errors

## Verification

- `python -c "from PIL import Image; img = Image.open('Resources/app.ico'); print(f'Icon sizes: {img.info.get(\"sizes\", \"unknown\")}')"` shows multiple sizes
- `powershell -Command "Select-String -Path DocuFiller.csproj -Pattern 'ApplicationIcon'"` returns matching line
- `dotnet build` exits with code 0

## Observability Impact

No runtime observability changes — this is a static resource task.
  - Files: `Resources/app.ico`, `Resources/app.png`, `DocuFiller.csproj`
  - Verify: dotnet build && python -c "from PIL import Image; img=Image.open('Resources/app.ico'); print('OK:', img.size)"

- [x] **T02: Set Icon on all windows and replace emoji with Image in MainWindow title bar** `est:20m`
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
  - Files: `MainWindow.xaml`, `DocuFiller/Views/CleanupWindow.xaml`, `DocuFiller/Views/DownloadProgressWindow.xaml`, `DocuFiller/Views/UpdateSettingsWindow.xaml`
  - Verify: dotnet build && dotnet test

## Files Likely Touched

- Resources/app.ico
- Resources/app.png
- DocuFiller.csproj
- MainWindow.xaml
- DocuFiller/Views/CleanupWindow.xaml
- DocuFiller/Views/DownloadProgressWindow.xaml
- DocuFiller/Views/UpdateSettingsWindow.xaml
