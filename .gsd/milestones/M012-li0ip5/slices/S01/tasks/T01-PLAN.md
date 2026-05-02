---
estimated_steps: 42
estimated_files: 2
skills_used: []
---

# T01: 关键词替换 Tab 紧凑化与拖放迁移

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

## Inputs

- ``MainWindow.xaml` — 当前 1400x900 布局，Tab 1 含三个 GroupBox 和两个拖放 Border`
- ``MainWindow.xaml.cs` — 当前拖放事件处理器绑定到 Border 控件`
- ``ViewModels/MainWindowViewModel.cs` — 提供 BrowseTemplateCommand/BrowseDataCommand/BrowseOutputCommand 等命令和 TemplatePath/DataPath 等绑定属性`
- ``App.xaml` — 全局样式（GroupBoxStyle/PrimaryButton/TextBoxStyle）`

## Expected Output

- ``MainWindow.xaml` — 紧凑化布局：900x550 窗口、Tab 1 无 GroupBox、TextBox 支持 AllowDrop、浏览按钮`
- ``MainWindow.xaml.cs` — 拖放事件处理器迁移到 TextBox、视觉反馈适配`

## Verification

dotnet build && grep -c "AllowDrop" MainWindow.xaml
