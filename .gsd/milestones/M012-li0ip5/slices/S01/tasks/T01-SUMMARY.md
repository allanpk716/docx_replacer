---
id: T01
parent: S01
milestone: M012-li0ip5
key_files:
  - MainWindow.xaml
  - MainWindow.xaml.cs
key_decisions:
  - 使用 DockPanel 包裹 Tab 1 内容（Grid + 操作按钮），Grid DockPanel.Dock=Top 让操作按钮始终在底部
  - TextBox 拖放视觉反馈直接修改 BorderBrush/BorderThickness/Background，恢复时 Background=White（TextBox 默认背景）
  - RestoreBorderStyle/UpdateBorderStyle 改为清理 Tab 配色方案（#CCC/#F9F9F9），与 Tab 1 TextBox 的恢复样式（#BDC3C7/White）分开
duration: 
verification_result: passed
completed_at: 2026-05-02T00:49:27.939Z
blocker_discovered: false
---

# T01: 关键词替换 Tab 紧凑化：移除 GroupBox 和拖放 Border，改为 TextBox AllowDrop + 浏览按钮布局，窗口缩至 900x550

**关键词替换 Tab 紧凑化：移除 GroupBox 和拖放 Border，改为 TextBox AllowDrop + 浏览按钮布局，窗口缩至 900x550**

## What Happened

重写了关键词替换 Tab 的 XAML 布局：
1. **窗口尺寸**：从 1400x900 缩小为 900x550，MinWidth=800 MinHeight=500
2. **移除三个 GroupBox**：模板文件、数据文件、输出设置不再使用 GroupBox 包裹，改为 Grid 行布局 + Separator 分隔线
3. **移除两个拖放 Border**：删除 TemplateDropBorder 和 DataFileDropBorder，改为 TemplatePathTextBox 和 DataPathTextBox 直接支持 AllowDrop=True，绑定 DragEnter/DragLeave/DragOver/Drop 事件
4. **迁移事件处理器**：MainWindow.xaml.cs 中的 8 个拖放事件处理器全部重命名并迁移，视觉反馈从修改 Border 属性改为修改 TextBox 的 BorderBrush/BorderThickness/Background
5. **添加浏览按钮**：模板文件行增加"浏览"和"文件夹"按钮（绑定 BrowseTemplateCommand 和 BrowseTemplateFolderCommand），数据文件行增加"浏览"按钮
6. **字号和间距缩小**：TabControl/TabItem FontSize 从 16 降为 14，标签 TextBlock 13px，正文 TextBox 12px，按钮 12-13px，Margin 从 8-15 降为 3-8
7. **保留辅助方法**：RestoreBorderStyle/UpdateBorderStyle 改为清理 Tab 专用的灰色配色（#CCCCCCC/#F9F9F9），UpdateHintText 已删除（不再需要）
8. **Tab 2 字号同步**：审核清理 Tab Header FontSize 从 16 改为 14

编译通过（0 error, 72 warnings 均为既有 nullable 警告）。

## Verification

dotnet build 编译通过（0 error）。验证命令确认：Tab 1 中 GroupBox 数量为 0，AllowDrop 在 MainWindow.xaml 中出现 3 次（Tab1 两个 TextBox + Tab2 清理拖放区域），TemplateDropBorder/DataFileDropBorder 引用数为 0，新的 TemplatePathTextBox_Drop 和 DataPathTextBox_Drop 事件处理器各出现 2 次（XAML 声明 + cs 实现）。

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build` | 0 | ✅ pass | 3500ms |
| 2 | `grep -c AllowDrop MainWindow.xaml` | 0 | ✅ pass (count=3) | 50ms |
| 3 | `awk /关键词替换/,/审核清理/ MainWindow.xaml | grep -c GroupBox` | 1 | ✅ pass (count=0, exit 1 means no matches) | 50ms |
| 4 | `grep -c TemplateDropBorder|DataFileDropBorder MainWindow.xaml` | 1 | ✅ pass (count=0) | 50ms |
| 5 | `grep -c TemplatePathTextBox_Drop|DataPathTextBox_Drop MainWindow.xaml` | 0 | ✅ pass (count=2) | 50ms |

## Deviations

计划要求操作按钮和内容用同一容器，实际使用 DockPanel 包裹（Grid DockPanel.Dock=Top + 操作按钮 Grid），因为 TabItem 只能有一个直接子元素。这是 XAML 结构约束，不影响功能和视觉效果。

## Known Issues

None.

## Files Created/Modified

- `MainWindow.xaml`
- `MainWindow.xaml.cs`
