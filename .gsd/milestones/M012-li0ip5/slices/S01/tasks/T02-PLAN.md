---
estimated_steps: 27
estimated_files: 2
skills_used: []
---

# T02: 审核清理 Tab 紧凑化与编译验证

将审核清理 Tab 的布局同步紧凑化（去 GroupBox、降字号、压间距），与关键词替换 Tab 风格一致。同时调整 App.xaml 全局样式参数，最终 dotnet build 验证编译通过。

## Steps
1. 修改 Tab 2 (审核清理) XAML 布局：
   - 移除 GroupBox"输出设置"，改为标签 + 行内布局
     - 输出目录行：TextBlock"输出目录" + TextBox(CleanupOutputDirectory) + Button"浏览" + Button"打开文件夹"
   - 压缩拖放区域：保持 CleanupDropZoneBorder 但减少 Padding（从 30 降到 10-15）和整体高度
   - 文件列表区域保持但缩小按钮和字号
   - 进度区：TextBlock + ProgressBar（去掉外层 StackPanel 的多余 Margin）
   - 按钮：缩小高度 30-32px
2. 调整 Tab 2 字号与 Tab 1 一致（标签 13px、正文 12px、按钮 12-13px）
3. TabControl 的 FontSize 统一设为 14（Tab 标题字号）
4. App.xaml 样式调整（如需要）：
   - ModernTextBoxStyle 的 Padding 可从 12,8 降到 8,4
   - HeaderLabelStyle FontSize 从 16 降到 13
   - GroupBoxStyle 的 Padding 从 12 降到 8（供仍使用 GroupBox 的子窗口）
   - 按钮样式 Padding 从 16,8 降到 12,6
5. 运行 `dotnet build` 确认编译通过，修复任何错误

## Must-Haves
- [ ] Tab 2 无 GroupBox 元素
- [ ] Tab 2 字号与 Tab 1 一致（12-14px）
- [ ] CleanupDropZoneBorder 拖放功能不受影响
- [ ] 两个 Tab 视觉风格统一
- [ ] dotnet build 编译通过，0 errors

## Verification
- `dotnet build` 编译通过
- Tab 2 无 GroupBox
- 全局字号范围 12-14px

## Inputs

- ``MainWindow.xaml` — T01 产出：Tab 1 已紧凑化，Tab 2 仍为旧布局`
- ``App.xaml` — 当前全局样式，字号和 Padding 偏大`
- ``MainWindow.xaml.cs` — T01 产出：拖放处理器已迁移`

## Expected Output

- ``MainWindow.xaml` — Tab 2 紧凑化布局完成，两个 Tab 风格统一`
- ``App.xaml` — 调整后的全局样式（字号和 Padding 适配紧凑布局）`

## Verification

dotnet build
