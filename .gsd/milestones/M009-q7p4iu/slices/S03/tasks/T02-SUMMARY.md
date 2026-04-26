---
id: T02
parent: S03
milestone: M009-q7p4iu
key_files:
  - MainWindow.xaml
key_decisions:
  - 使用 TextBlock.InputBindings + MouseBinding 而非 code-behind 事件处理实现点击交互，保持 XAML 纯声明式绑定模式
  - 更新提示带下划线 TextDecoration（颜色绑定 UpdateStatusBrush），视觉上暗示可点击
duration: 
verification_result: passed
completed_at: 2026-04-26T11:18:42.749Z
blocker_discovered: false
---

# T02: MainWindow.xaml 状态栏新增常驻更新提示 TextBlock，绑定 ViewModel 的 UpdateStatusMessage/UpdateStatusBrush/HasUpdateStatus，支持点击和颜色变化

**MainWindow.xaml 状态栏新增常驻更新提示 TextBlock，绑定 ViewModel 的 UpdateStatusMessage/UpdateStatusBrush/HasUpdateStatus，支持点击和颜色变化**

## What Happened

修改 MainWindow.xaml 底部状态栏 Grid 布局，从 3 列扩展为 4 列，在版本号（Column 0）和中间消息（Column 1）之后、检查更新按钮之前，新增常驻更新提示 TextBlock（Column 2，x:Name="UpdateStatusText"）。

具体实现：
1. Grid.ColumnDefinitions 新增第 4 列（Auto 宽度），原"检查更新"按钮从 Column 2 移至 Column 3。
2. 新增 TextBlock 绑定：
   - Text → UpdateStatusMessage（状态文本：便携版/有更新/最新/检查中/错误）
   - Foreground → UpdateStatusBrush（橙色/绿色/灰色/红色）
   - Visibility → HasUpdateStatus 通过 BooleanToVisibilityConverter（None 状态时隐藏）
3. 点击交互：使用 TextBlock.InputBindings + MouseBinding（LeftClick）绑定 UpdateStatusClickCommand，无需 code-behind 事件处理。
4. 视觉样式：FontSize=12、Cursor=Hand、Margin=15,0,15,0，带下划线 TextDecoration（颜色绑定 UpdateStatusBrush）。

构建验证：dotnet build -c Release 0 错误，dotnet test 全部 172 个测试通过（27 E2E + 145 单元测试）。

## Verification

执行 dotnet build -c Release 编译成功（0 error CS / 0 error MC），dotnet test --no-build 全部 172 测试通过（27 E2ERegression + 145 DocuFiller.Tests）。

XAML 绑定验证：
- UpdateStatusText.Text 绑定 UpdateStatusMessage
- UpdateStatusText.Foreground 绑定 UpdateStatusBrush
- UpdateStatusText.Visibility 绑定 HasUpdateStatus + BooleanToVisibilityConverter
- MouseBinding LeftClick 绑定 UpdateStatusClickCommand
- 检查更新按钮 Grid.Column 从 2 改为 3，布局正确

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build -c Release` | 0 | ✅ pass | 2500ms |
| 2 | `dotnet test --no-build -c Release` | 0 | ✅ pass | 16000ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `MainWindow.xaml`
