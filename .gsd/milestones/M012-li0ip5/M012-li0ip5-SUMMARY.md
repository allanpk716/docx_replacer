---
id: M012-li0ip5
title: "主界面布局紧凑化与拖放修复"
status: complete
completed_at: 2026-05-02T01:39:34.301Z
key_decisions:
  - DockPanel 包裹 TabItem 内容（Grid DockPanel.Dock=Top + 底部按钮），解决 TabItem 单子元素约束
  - 拖放区域从独立 Border 迁移到 TextBox AllowDrop，节省约 150px 垂直空间
  - 三个 GroupBox 替换为 TextBlock + Separator，节省约 90px
  - 窗口默认尺寸从 1400x900 降至 900x550
  - 字号分层：TabControl=14, labels=13, body/buttons=12
  - Window 级 AllowDrop + PreviewDragOver Activate() 修复未聚焦拖放
key_files:
  - MainWindow.xaml
  - MainWindow.xaml.cs
  - App.xaml
lessons_learned:
  - WPF TabItem 只允许一个直接子元素，需要 DockPanel/Grid 包裹多个内容区域
  - WPF 拖放在窗口未聚焦时需要 Window 级 AllowDrop + Activate() 来注册 OLE 拖放目标
  - PreviewDragOver 隧道事件需要 e.Handled=false 才能让子控件也接收到事件
---

# M012-li0ip5: 主界面布局紧凑化与拖放修复

**主界面从 1400x900 紧凑化为 900x550（GroupBox 移除、字号降至 12-14px），拖放区域迁移到 TextBox AllowDrop，窗口未聚焦拖放通过 Window 级 PreviewDragOver+Activate 修复**

## What Happened

## Cross-Slice Narrative

M012 实现了 DocuFiller 主界面的全面紧凑化和拖放焦点修复两个目标。

**S01（布局紧凑化）**重写了两个 Tab 的全部布局。关键词替换 Tab 移除了三个 GroupBox 和两个拖放 Border（共节省约 270px 垂直空间），改为 Grid 行布局 + Separator 分隔线。拖放功能迁移到路径 TextBox 的 AllowDrop 机制，在 MainWindow.xaml.cs 实现了 DragEnter/DragLeave/DragOver/Drop 事件处理器。窗口尺寸从 1400x900 降至 900x550（MinWidth=800 MinHeight=500）。审核清理 Tab（T02）同步紧凑化，CleanupDropZoneBorder Padding 从 30 降为 12，按钮和进度条缩小。App.xaml 全局样式调整了 Padding 和 FontSize。关键适配：TabItem 只能有一个直接子元素，因此使用 DockPanel 包裹结构（Grid DockPanel.Dock=Top + 底部操作按钮 Grid）。

**S02（拖放焦点修复）**在 Window 元素上添加 AllowDrop="True" 和 PreviewDragOver="Window_PreviewDragOver"，当窗口未聚焦时调用 Activate() 将窗口注册为 OLE 拖放目标。PreviewDragOver 设置 e.Handled=false 让隧道事件继续传递给子控件。T02 验证了所有 4 个 AllowDrop 目标和全部 14 个拖放事件处理器完整保留。

两个切片紧密协作：S01 建立了紧凑布局和 TextBox 拖放基础，S02 在此基础上添加 Window 级激活机制。最终在 1366x768 分辨率下，两个 Tab 所有控件完整可见无需滚动，窗口未聚焦时拖放也能正常触发。

## Success Criteria Results

- ✅ **1366x768 下两个 Tab 所有控件完整可见无需滚动** — Window 900x550 (MinWidth=800 MinHeight=500)，三个 GroupBox 全部移除（grep 确认 0 个 GroupBox），字号分层 12-14px，审核清理 Tab 同步紧凑化
- ✅ **窗口未聚焦时拖放正常工作** — Window AllowDrop=True + PreviewDragOver handler 调用 Activate()，所有 4 个 AllowDrop 目标和 14 个拖放事件处理器完整
- ✅ **dotnet build 编译通过** — 0 errors, 0 warnings
- ✅ **现有功能不受影响** — 纯 UI 改动（XAML + code-behind 事件），ViewModel、Services、CLI 均未修改

## Definition of Done Results

- ✅ S01 complete — 2/2 tasks done, summary written
- ✅ S02 complete — 2/2 tasks done, summary written
- ✅ All slice summaries exist at .gsd/milestones/M012-li0ip5/slices/S01/S01-SUMMARY.md and S02/S02-SUMMARY.md
- ✅ Cross-slice integration: S01 的 TextBox 拖放基础 + S02 的 Window 级激活机制协同工作

## Requirement Outcomes

- R050: active → validated — Window 900x550, 两个 Tab 紧凑化布局，GroupBox 全移除，12-14px 字号，dotnet build 0 errors
- R051: active → validated — TemplateDropBorder/DataFileDropBorder 移除，改为 TextBox AllowDrop + 事件处理器
- R052: active → validated — TabControl=14, labels=13, body=12, App.xaml 全局样式调整
- R053: active → validated — 三个 GroupBox 移除，替换为 TextBlock + Separator
- R054: active → validated — Window AllowDrop=True + PreviewDragOver Activate()，4 个 AllowDrop 目标和 14 个事件处理器完整
- R055: active → validated — 审核清理 Tab 使用相同 DockPanel 结构和字号分层

## Deviations

None.

## Follow-ups

None.
