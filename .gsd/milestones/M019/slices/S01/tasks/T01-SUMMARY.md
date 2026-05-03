---
id: T01
parent: S01
milestone: M019
key_files:
  - App.xaml
key_decisions:
  - 使用 Grid 包裹 PART_Track + PART_Indicator 双 Rectangle 方案，而非仅修复单个 Rectangle，符合 WPF ProgressBar TemplatePart 标准约定
duration: 
verification_result: passed
completed_at: 2026-05-03T11:43:02.768Z
blocker_discovered: false
---

# T01: 修复 ModernProgressBarStyle 模板：添加 PART_Indicator 元素使进度条填充区域随 Value 值可见增长

**修复 ModernProgressBarStyle 模板：添加 PART_Indicator 元素使进度条填充区域随 Value 值可见增长**

## What Happened

分析发现 ModernProgressBarStyle 的 ControlTemplate 只有 PART_Track（且错误地绑定了 Foreground 填充色），缺少 WPF ProgressBar 标准要求的 PART_Indicator 元素。WPF ProgressBar 内部通过 TemplatePart 名称约定查找 PART_Indicator，并根据 Value/Minimum/Maximum 比例自动设置其宽度。

修复内容：
1. 在 ControlTemplate 的 Border 内添加 Grid 包裹两个 Rectangle
2. PART_Track 改为 Fill="Transparent"（Border 已提供轨道背景色 #E0E0E0）
3. 新增 PART_Indicator，Fill 绑定 Foreground（PrimaryBrush 蓝色），HorizontalAlignment=Left
4. 两个 Rectangle 都保持 RadiusX/RadiusY=4 圆角，与 Border CornerRadius=4 一致

构建验证：dotnet build 成功，0 错误。grep 确认 PART_Track 和 PART_Indicator 两个命名元素均存在。

## Verification

1. `dotnet build` — 0 错误，构建成功
2. `grep -c 'PART_Track\|PART_Indicator' App.xaml` — 返回 2，确认两个命名元素存在
3. 手动检查模板结构：Grid 包裹 PART_Track（透明）+ PART_Indicator（Foreground 填充，左对齐），符合 WPF ProgressBar 标准模式

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build` | 0 | ✅ pass | 8710ms |
| 2 | `grep -c 'PART_Track|PART_Indicator' App.xaml` | 0 | ✅ pass (count=2) | 50ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `App.xaml`
