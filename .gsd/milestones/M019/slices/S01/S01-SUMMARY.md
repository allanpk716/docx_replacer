---
id: S01
parent: M019
milestone: M019
provides:
  - ["修复后的 ModernProgressBarStyle 模板（App.xaml），包含 PART_Indicator"]
requires:
  []
affects:
  []
key_files:
  - ["App.xaml"]
key_decisions:
  - ["使用 Grid 包裹 PART_Track + PART_Indicator 双 Rectangle 方案，符合 WPF ProgressBar TemplatePart 标准约定", "PART_Track 改为 Fill=Transparent，由外层 Border 提供轨道背景色"]
patterns_established:
  - ["WPF ProgressBar ControlTemplate 标准模式：Border → Grid → PART_Track + PART_Indicator", "Windows 验证使用 PowerShell Select-String 替代 grep"]
observability_surfaces:
  - none
drill_down_paths:
  - [".gsd/milestones/M019/slices/S01/tasks/T01-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-05-03T11:43:42.918Z
blocker_discovered: false
---

# S01: 修复进度条可见增长

**修复 ModernProgressBarStyle 模板添加 PART_Indicator 元素，使进度条填充区域随 Value 值从左向右可见增长**

## What Happened

分析发现 ModernProgressBarStyle 的 ControlTemplate 只有 PART_Track（且错误地绑定了 Foreground 填充色），缺少 WPF ProgressBar 标准要求的 PART_Indicator 元素。WPF ProgressBar 内部通过 TemplatePart 名称约定查找 PART_Indicator，并根据 Value/Minimum/Maximum 比例自动设置其宽度。缺少此元素时，无论 Value 如何变化都无可见填充。

修复内容：
1. 在 ControlTemplate 的 Border 内添加 Grid 包裹两个 Rectangle
2. PART_Track 改为 Fill="Transparent"（Border 已提供轨道背景色 #E0E0E0）
3. 新增 PART_Indicator，Fill 绑定 Foreground（PrimaryBrush 蓝色），HorizontalAlignment=Left
4. 两个 Rectangle 都保持 RadiusX/RadiusY=4 圆角，与 Border CornerRadius=4 一致

构建验证：dotnet build 成功，0 错误。PowerShell Select-String 确认 PART_Track（行 169）和 PART_Indicator（行 173）两个命名元素均存在。

## Verification

1. dotnet build — 0 错误，构建成功
2. powershell Select-String 'PART_Indicator|PART_Track' App.xaml — 确认两个命名元素存在于行 169 和 173
3. 手动审查模板结构：Grid 包裹 PART_Track（透明）+ PART_Indicator（Foreground 填充，左对齐），符合 WPF ProgressBar 标准模式

## Requirements Advanced

- R046 — ModernProgressBarStyle 模板添加 PART_Indicator 元素，WPF ProgressBar 自动管理填充宽度

## Requirements Validated

- R046 — 模板包含 PART_Track 和 PART_Indicator 双命名元素，dotnet build 通过，PowerShell 确认两元素存在

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

None.
