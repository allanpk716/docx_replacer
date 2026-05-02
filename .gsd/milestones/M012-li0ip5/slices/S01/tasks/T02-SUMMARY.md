---
id: T02
parent: S01
milestone: M012-li0ip5
key_files:
  - MainWindow.xaml
  - App.xaml
key_decisions:
  - Tab 2 使用与 Tab 1 相同的 DockPanel 包裹结构（Grid DockPanel.Dock=Top + 操作按钮 Grid），保持布局一致性
duration: 
verification_result: passed
completed_at: 2026-05-02T00:52:51.912Z
blocker_discovered: false
---

# T02: 审核清理 Tab 紧凑化：移除 GroupBox、降字号至 12-14px、压缩间距和按钮尺寸，同步调整 App.xaml 全局样式

**审核清理 Tab 紧凑化：移除 GroupBox、降字号至 12-14px、压缩间距和按钮尺寸，同步调整 App.xaml 全局样式**

## What Happened

将审核清理 Tab (Tab 2) 的布局紧凑化，与 T01 已完成的关键词替换 Tab 风格统一：

1. **移除 GroupBox**：删除"输出设置"GroupBox，改为与 Tab 1 一致的标签 + 行内布局（TextBlock 65px 宽 + TextBox + 按钮）
2. **输出目录行**：重写为内联布局，字号 13px 标签 + 12px TextBox + 55x28 "浏览"按钮 + 75x28 "打开文件夹"按钮
3. **拖放区域压缩**：CleanupDropZoneBorder 的 Padding 从 30 降为 12，提示文字从 14px 降为 12px，添加 CornerRadius=3
4. **文件列表紧凑化**：ListView 字号 12px，列宽缩小（40→35, 300→250, 100→80, 150→120），按钮从 100x30 降为 70x26
5. **进度区**：字号 12px，ProgressBar 高度从 25 降为 16，配色与 Tab 1 一致
6. **操作按钮**：改为与 Tab 1 相同的 DockPanel 底部布局，100x32 "开始清理" + 70x32 "退出"
7. **App.xaml 全局样式调整**：ModernTextBoxStyle Padding 从 12,8→8,4，三个按钮样式 Padding 从 16,8→12,6，HeaderLabelStyle FontSize 从 16→13，GroupBoxStyle Padding 从 12→8

编译通过，0 error 0 warning。

## Verification

dotnet build 编译通过（0 error, 0 warning）。验证确认：MainWindow.xaml 中无 GroupBox 引用（findstr 返回空），AllowDrop 出现 3 次（Tab1 两个 TextBox + Tab2 拖放区域），CleanupDropZoneBorder 拖放事件处理器完整保留，所有 FontSize 在 11-14px 范围内，两个 Tab 使用一致的字号和布局模式。

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build` | 0 | ✅ pass | 2340ms |
| 2 | `findstr /C:GroupBox MainWindow.xaml` | 1 | ✅ pass (0 GroupBox found) | 50ms |
| 3 | `findstr AllowDrop MainWindow.xaml` | 0 | ✅ pass (3 occurrences) | 50ms |
| 4 | `findstr CleanupDropZoneBorder MainWindow.xaml` | 0 | ✅ pass (drag handlers intact) | 50ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `MainWindow.xaml`
- `App.xaml`
