---
estimated_steps: 5
estimated_files: 3
skills_used: []
---

# T02: Wire gear button into status bar, display source type, register window in DI

**Slice:** S02 — 更新设置弹窗 + 状态栏源类型显示
**Milestone:** M010-hpylzg

## Description

Add a gear icon button (⚙) to MainWindow.xaml status bar. Add OpenUpdateSettingsCommand to MainWindowViewModel that opens UpdateSettingsWindow as dialog. Modify UpdateStatusMessage getter to append source type suffix. Register window and ViewModel in DI. Ensure status bar refreshes after settings change.

## Steps

1. Modify `ViewModels/MainWindowViewModel.cs`:
   - Add a new command property: `public ICommand OpenUpdateSettingsCommand { get; private set; } = null!;`
   - In `InitializeCommands()`, initialize: `OpenUpdateSettingsCommand = new RelayCommand(OpenUpdateSettings);`
   - Add `OpenUpdateSettings()` method:
     - Get `UpdateSettingsWindow` from `((App)Application.Current).ServiceProvider.GetRequiredService<Views.UpdateSettingsWindow>()`
     - Set `Owner = Application.Current.MainWindow`
     - Call `ShowDialog()`
     - If dialog returned true (user saved), call `OnPropertyChanged(nameof(UpdateStatusMessage))` to refresh the status bar display
   - Modify `UpdateStatusMessage` getter to append source type suffix:
     - After the existing switch expression, if `_updateService != null` and `_updateStatus` is not `None` and not `Checking`, append:
       - If `_updateService.UpdateSourceType == "GitHub"`: append `" (GitHub)"`
       - If `_updateService.UpdateSourceType == "HTTP"`: extract host from `_updateService.EffectiveUpdateUrl` (strip protocol and trailing path) and append `" (内网: {host})"`
     - Example: "当前已是最新版本" → "当前已是最新版本 (GitHub)" or "当前已是最新版本 (内网: 192.168.1.100:8080)"
   - Helper to extract host: simple string manipulation — strip "http://" or "https://", then split on '/' and take first part, then strip trailing port if desired (or keep it). E.g. `"http://192.168.1.100:8080/stable/"` → `"192.168.1.100:8080"`

2. Modify `MainWindow.xaml` status bar:
   - Add a new ColumnDefinition between the UpdateStatusText column (Column 2) and the "检查更新" button column (Column 3)
   - Update column indices: the gear button goes in the new column, the "检查更新" button shifts to the next column
   - Add gear button in the new column:
     ```xml
     <Button Grid.Column="3" Content="⚙" ToolTip="更新源设置"
             Command="{Binding OpenUpdateSettingsCommand}"
             FontSize="16" Padding="8,2" Cursor="Hand"
             Background="Transparent" BorderBrush="#CCCCCC" BorderThickness="1"
             Foreground="#555555" VerticalAlignment="Center" Margin="5,0,5,0">
         <Button.Template>
             <ControlTemplate TargetType="Button">
                 <Border Background="{TemplateBinding Background}"
                         CornerRadius="3"
                         BorderBrush="{TemplateBinding BorderBrush}"
                         BorderThickness="{TemplateBinding BorderThickness}"
                         Padding="{TemplateBinding Padding}">
                     <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                 </Border>
                 <ControlTemplate.Triggers>
                     <Trigger Property="IsMouseOver" Value="True">
                         <Setter Property="Background" Value="#E8E8E8"/>
                     </Trigger>
                 </ControlTemplate.Triggers>
             </ControlTemplate>
         </Button.Template>
     </Button>
     ```
   - Update "检查更新" button to `Grid.Column="4"` (was Column 3)

3. Modify `App.xaml.cs` DI registration:
   - Add after the existing `services.AddTransient<Views.CleanupWindow>();` line:
     ```csharp
     services.AddTransient<Views.UpdateSettingsWindow>();
     services.AddTransient<UpdateSettingsViewModel>();
     ```

4. Run `dotnet build` and confirm 0 CS/MC errors

5. Verify no regression in existing update flow: all existing tests should pass (`dotnet test`)

## Must-Haves

- [ ] Gear button (⚙) visible in status bar between update status text and "检查更新" button
- [ ] Clicking gear button opens UpdateSettingsWindow as modal dialog
- [ ] UpdateStatusMessage appends source type suffix (GitHub or 内网)
- [ ] After saving settings in UpdateSettingsWindow, status bar message updates immediately
- [ ] UpdateSettingsWindow and UpdateSettingsViewModel registered in DI container

## Verification

- `dotnet build` returns 0 CS/MC errors
- `dotnet test` passes with no regressions (all existing tests pass)
- MainWindow.xaml contains `OpenUpdateSettingsCommand` binding
- UpdateStatusMessage getter references `_updateService.UpdateSourceType`

## Inputs

- `Services/Interfaces/IUpdateService.cs` — UpdateSourceType, EffectiveUpdateUrl, Channel, ReloadSource
- `DocuFiller/Views/UpdateSettingsWindow.xaml` — new window from T01
- `DocuFiller/Views/UpdateSettingsWindow.xaml.cs` — new window code-behind from T01
- `ViewModels/UpdateSettingsViewModel.cs` — new ViewModel from T01
- `MainWindow.xaml` — existing status bar to modify
- `ViewModels/MainWindowViewModel.cs` — existing ViewModel to add command and modify UpdateStatusMessage
- `App.xaml.cs` — existing DI registration to extend

## Expected Output

- `MainWindow.xaml` — status bar with gear button added
- `ViewModels/MainWindowViewModel.cs` — OpenUpdateSettingsCommand + source type suffix in UpdateStatusMessage
- `App.xaml.cs` — DI registration for UpdateSettingsWindow and UpdateSettingsViewModel
