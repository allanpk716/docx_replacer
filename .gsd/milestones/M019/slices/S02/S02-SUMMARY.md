---
id: S02
parent: M019
milestone: M019
provides:
  - (none)
requires:
  []
affects:
  []
key_files:
  - ["Resources/app.ico", "Resources/app.png", "DocuFiller.csproj", "MainWindow.xaml", "DocuFiller/Views/CleanupWindow.xaml", "DocuFiller/Views/DownloadProgressWindow.xaml", "DocuFiller/Views/UpdateSettingsWindow.xaml", "generate_icon.py"]
key_decisions:
  - ["手动构建 ICO 二进制文件（而非使用 Pillow 的 ICO 保存器），因为后者只保存第一帧", "保留 generate_icon.py 在项目根目录供未来图标重新生成", "MainWindow 标题栏 emoji 替换为 16x16 Image 控件并使用 HighQuality 缩放模式"]
patterns_established:
  - ["WPF 窗口图标使用 pack URI (pack://application:,,,/Resources/app.ico) 统一引用", "自定义标题栏图标使用 Image 控件 + RenderOptions.BitmapScalingMode=\"HighQuality\"", "应用图标通过 csproj ApplicationIcon 嵌入 exe，同时通过 Resource Include 供 XAML 引用"]
observability_surfaces:
  - none
drill_down_paths:
  - [".gsd/milestones/M019/slices/S02/tasks/T01-SUMMARY.md", ".gsd/milestones/M019/slices/S02/tasks/T02-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-05-03T11:54:06.964Z
blocker_discovered: false
---

# S02: 应用图标创建与应用

**生成 DocuFiller 专属应用图标（多分辨率 .ico + .png），应用到 exe 资源、所有窗口标题栏和任务栏，替换 MainWindow 自定义标题栏中的 emoji 为真实图标控件。**

## What Happened

## 概述

S02 完成了 DocuFiller 应用图标的完整创建和应用流程，共 2 个任务。

## T01: 图标生成与 csproj 配置

使用 Python/Pillow 脚本程序化生成专业应用图标。设计为蓝色 Word 文档（带折叠角）+ 绿色对勾徽章，象征"文档已填充"。生成了 256x256 PNG 主图和包含 6 个分辨率（16/32/48/64/128/256px）的 .ico 文件。

**关键发现**：Pillow 内置 ICO 保存器只写入第一帧，因此手动按 ICO 规范构建了二进制文件（PNG 编码帧）。脚本 `generate_icon.py` 保留在项目根目录供未来重新生成。

在 `DocuFiller.csproj` 中添加了 `<ApplicationIcon>Resources\app.ico</ApplicationIcon>`，图标通过已有的 `<Resource Include="Resources\**" />` 自动作为 WPF Resource 可用。

## T02: 窗口图标应用与 emoji 替换

为 4 个 WPF 窗口设置 `Icon` 属性（pack URI）：
- **MainWindow.xaml**: Window.Icon + 标题栏 emoji 📄 替换为 16x16 Image 控件（HighQuality 缩放）
- **CleanupWindow.xaml**: Window.Icon
- **DownloadProgressWindow.xaml**: Window.Icon
- **UpdateSettingsWindow.xaml**: Window.Icon

**修复**：发现 MainWindow.xaml 有重复的闭合标签导致 MC3000 构建错误，修复了 XML 结构损坏。

## Verification

## 验证结果（全部通过）

| # | 检查项 | 命令 | 结果 |
|---|--------|------|------|
| 1 | app.ico 存在且含多分辨率 | `python -c "from PIL import Image; ..."` | ✅ 6 sizes: {16,32,48,64,128,256} |
| 2 | app.png 存在 | `ls Resources/app.png` | ✅ 1967 bytes |
| 3 | csproj ApplicationIcon | `Select-String DocuFiller.csproj` | ✅ 第 10 行匹配 |
| 4 | MainWindow Icon 属性 | `Select-String MainWindow.xaml` | ✅ 2 处匹配（Window + Image） |
| 5 | CleanupWindow Icon | `Select-String CleanupWindow.xaml` | ✅ 匹配 |
| 6 | DownloadProgressWindow Icon | `Select-String DownloadProgressWindow.xaml` | ✅ 匹配 |
| 7 | UpdateSettingsWindow Icon | `Select-String UpdateSettingsWindow.xaml` | ✅ 匹配 |
| 8 | emoji 已移除 | `grep 📄 MainWindow.xaml` | ✅ 0 匹配 |
| 9 | dotnet build | `dotnet build` | ✅ 0 错误, 0 警告 |
| 10 | dotnet test | `dotnet test` | ✅ 249/249 通过 (222 + 27) |

## Requirements Advanced

None.

## Requirements Validated

None.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

- `Resources/app.ico` — 新增多分辨率应用图标（6 sizes: 16-256px）
- `Resources/app.png` — 新增 256x256 PNG 图标
- `DocuFiller.csproj` — 添加 ApplicationIcon 属性
- `MainWindow.xaml` — 添加 Window.Icon 属性，标题栏 emoji 替换为 Image 控件，修复 XML 重复闭合标签
- `DocuFiller/Views/CleanupWindow.xaml` — 添加 Window.Icon 属性
- `DocuFiller/Views/DownloadProgressWindow.xaml` — 添加 Window.Icon 属性
- `DocuFiller/Views/UpdateSettingsWindow.xaml` — 添加 Window.Icon 属性
- `generate_icon.py` — 新增图标生成脚本
