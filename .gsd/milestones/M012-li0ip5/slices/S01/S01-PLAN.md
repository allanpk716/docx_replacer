# S01: 主界面布局紧凑化

**Goal:** 将 DocuFiller 主界面从 1400x900 松散布局紧凑化为 900x550，关键词替换 Tab 去掉三个 GroupBox 和两个拖放 Border（改为 TextBox AllowDrop），审核清理 Tab 同步紧凑化，确保 1366x768 下两个 Tab 无需滚动即可完整显示。
**Demo:** 启动应用在 1366x768 下看到完整紧凑的界面，两个 Tab 内容无需滚动即可看全

## Must-Haves

- Window Height=550 Width=900 MinHeight=500 MinWidth=800
- Tab 1 无 GroupBox，用标签+分隔线替代
- Tab 1 无独立拖放 Border，路径 TextBox 支持 AllowDrop
- Tab 1 每个路径栏旁有浏览按钮（BrowseTemplateCommand/BrowseDataCommand）
- Tab 2 同步紧凑化（无 GroupBox，字号 12-14px）
- 全局字号 TabControl=14、标签=13、正文=12
- dotnet build 编译通过
- 现有数据绑定（TemplatePath/DataPath/OutputDirectory 等）不受影响

## Proof Level

- This slice proves: contract

## Integration Closure

- Upstream surfaces consumed: MainWindowViewModel 绑定属性和命令（不改 ViewModel）
- New wiring introduced in this slice: TextBox AllowDrop 事件绑定到 code-behind handler
- What remains before the milestone is truly usable end-to-end: S02 需完成窗口未聚焦拖放修复

## Verification

- Signals added/changed: none
- How a future agent inspects this: dotnet build output
- Failure state exposed: build errors identify broken bindings or missing event handlers

## Tasks

- [x] **T01: 关键词替换 Tab 紧凑化与拖放迁移** `est:45m`
  重写关键词替换 Tab 的 XAML 布局：将三个 GroupBox 替换为标签+分隔线，移除两个拖放 Border，改为路径 TextBox 支持 AllowDrop 并添加浏览按钮。同时迁移 MainWindow.xaml.cs 中的拖放事件处理器从 Border 改为 TextBox。调整窗口尺寸为 900x550。

## Steps
1. 修改 Window 属性：Height=550 Width=900 MinHeight=500 MinWidth=800
2. 重写 Tab 1 (关键词替换) XAML 布局：
   - 移除 ScrollViewer 包裹
   - 移除三个 GroupBox，用 Grid 行布局替代
   - 模板文件行：TextBlock"模板文件" + TextBox(TemplatePath, AllowDrop=True) + Button"浏览"(BrowseTemplateCommand) + Button"文件夹"(BrowseTemplateFolderCommand)
   - 分隔线 Separator
   - 数据文件行：TextBlock"数据文件" + TextBox(DataPath, AllowDrop=True) + Button"浏览"(BrowseDataCommand)
   - 下方小字显示 TemplateFolderPath/FoundDocxFilesCount 和 DataFileTypeDisplay
   - 分隔线 Separator
   - 输出目录行：TextBlock"输出目录" + TextBox(OutputDirectory, IsReadOnly=True) + Button"浏览"(BrowseOutputCommand)
   - 处理进度区：TextBlock + ProgressBar（不包裹 GroupBox）
   - 操作按钮：保持原有三个按钮但缩小尺寸
3. 为三个 TextBox 添加 AllowDrop=True 和拖放事件（Drop/DragEnter/DragLeave/DragOver）
4. 修改 MainWindow.xaml.cs 拖放处理器：
   - TemplatePathTextBox 的 DragEnter/DragLeave/DragOver/Drop 事件（替代 TemplateDropBorder 事件）
   - DataPath TextBox 的 DragEnter/DragLeave/DragOver/Drop 事件（替代 DataFileDropBorder 事件）
   - 视觉反馈改为修改 TextBox 的 BorderBrush（因为 sender 是 TextBox 不是 Border）
   - 使用 Visual Tree 向上查找 Border 容器或直接修改 TextBox 的控件模板属性来提供视觉反馈
   - 或者使用单独的 Border 包裹 TextBox，在 Border 上提供视觉反馈
5. 删除不再需要的 TemplateDropBorder 和 DataFileDropBorder 相关代码（RestoreBorderStyle/UpdateBorderStyle/UpdateHintText 可保留供 Tab 2 使用）
6. 所有字号：TabControl FontSize=14，Tab 标签 Header=14，TextBlock 标签=13，正文 TextBox=12
7. 缩小所有 Margin（GroupBox 原来 Margin=8，改为紧凑间距 3-5）

## Must-Haves
- [ ] Tab 1 无 GroupBox 元素
- [ ] Tab 1 无独立拖放 Border 元素
- [ ] TemplatePath TextBox 支持 AllowDrop，DragEnter/DragOver/DragLeave/Drop 事件已绑定
- [ ] DataPath TextBox 支持 AllowDrop，DragEnter/DragOver/DragLeave/Drop 事件已绑定
- [ ] 模板和数据路径旁有浏览按钮
- [ ] 拖放视觉反馈正常（鼠标拖入时边框变色）
- [ ] 拖放功能正常（设置 ViewModel 属性并触发预览）
- [ ] 所有现有数据绑定完整保留
- [ ] Window 尺寸 900x550，MinWidth=800 MinHeight=500

## Verification
- `dotnet build` 编译通过
- Tab 1 无 GroupBox：`grep -c "GroupBox" MainWindow.xaml` 中 Tab 1 部分为 0（可手动检查）
- TextBox 有 AllowDrop：`grep -c "AllowDrop" MainWindow.xaml` >= 2

## Observability Impact
- Signals added/changed: none（纯 UI 变更）
- How a future agent inspects this: dotnet build 编译结果
- Failure state exposed: 编译错误会指出缺失的事件处理器或断裂的绑定
  - Files: `MainWindow.xaml`, `MainWindow.xaml.cs`
  - Verify: dotnet build && grep -c "AllowDrop" MainWindow.xaml

- [x] **T02: 审核清理 Tab 紧凑化与编译验证** `est:30m`
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
  - Files: `MainWindow.xaml`, `App.xaml`
  - Verify: dotnet build

## Files Likely Touched

- MainWindow.xaml
- MainWindow.xaml.cs
- App.xaml
