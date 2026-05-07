---
estimated_steps: 37
estimated_files: 1
skills_used: []
---

# T02: Add spinner rotation animation to status bar XAML

在 MainWindow.xaml 状态栏中添加 Canvas 元素实现的旋转 spinner 动画，绑定到 ShowCheckingAnimation 属性，在更新检查期间显示旋转动画。

## Steps
1. 在 `MainWindow.xaml` 的 Window 资源中添加 Storyboard：
   ```xml
   <Storyboard x:Key="SpinnerRotateAnimation" RepeatBehavior="Forever">
       <DoubleAnimation Storyboard.TargetProperty="(UIElement.RenderTransform).(RotateTransform.Angle)"
                        From="0" To="360" Duration="0:0:1"
                        RepeatBehavior="Forever"/>
   </Storyboard>
   ```
2. 在状态栏 Grid 中，将 Column 2 的内容改为一个 StackPanel（Horizontal），包含 spinner Canvas 和现有的 UpdateStatusText TextBlock。Spinner Canvas 使用一个 16x16 的 Canvas，内含 4 个弧形段（使用 ArcSegment 或简化的 Ellipse 路径），通过 RotateTransform 实现旋转。更简单的实现方式：使用一个带虚线描边的圆（Ellipse StrokeDashArray），通过 RotateTransform 旋转产生 spinner 效果。
3. Spinner 元素模板：
   ```xml
   <Canvas x:Name="SpinnerCanvas" Width="16" Height="16" Margin="0,0,5,0"
           Visibility="{Binding UpdateStatusVM.ShowCheckingAnimation, Converter={StaticResource BooleanToVisibilityConverter}}">
       <Canvas.RenderTransform>
           <RotateTransform CenterX="8" CenterY="8"/>
       </Canvas.RenderTransform>
       <Ellipse Width="16" Height="16" Stroke="#888888" StrokeThickness="2"
                StrokeDashArray="2 2.5" Fill="Transparent"/>
   </Canvas>
   ```
4. 添加 Loaded 事件触发动画的 EventTrigger，或使用 DataTrigger+Storyboard 在 ShowCheckingAnimation 变为 true 时启动动画。
5. 确保 spinner 在状态栏中的布局正确：版本号 | 进度消息 | [spinner] 更新状态文字 | ⚙ | 检查更新

## Must-Haves
- [ ] Canvas spinner 元素在 ShowCheckingAnimation=true 时可见并旋转
- [ ] 使用 BooleanToVisibilityConverter（已有 StaticResource）控制可见性
- [ ] 旋转动画流畅（1秒一圈，RepeatBehavior=Forever）
- [ ] 不影响现有状态栏布局
- [ ] dotnet build 零错误

## Verification
- `dotnet build` 返回 0 错误（过滤 External/update-client.exe 的预存在检查）

## Inputs
- `ViewModels/UpdateStatusViewModel.cs` — T01 添加的 ShowCheckingAnimation 属性
- `MainWindow.xaml` — 现有状态栏 XAML

## Expected Output
- `MainWindow.xaml` — 添加 spinner 元素和旋转动画资源

## Inputs

- `ViewModels/UpdateStatusViewModel.cs`
- `MainWindow.xaml`

## Expected Output

- `MainWindow.xaml`

## Verification

dotnet build 2>&1 | grep -v "update-client" | grep -i "error"
