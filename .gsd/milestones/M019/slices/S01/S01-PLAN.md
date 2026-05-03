# S01: 修复进度条可见增长

**Goal:** 修复 ModernProgressBarStyle 模板，添加 WPF 标准的 PART_Indicator 元素，使下载进度条填充区域随 Value 值从左向右可见增长
**Demo:** 下载进度条的填充区域随百分比数值增长从左向右平滑延伸

## Must-Haves

- ModernProgressBarStyle 模板包含名为 PART_Indicator 的元素（WPF 标准命名）
- PART_Track 仍然存在且作为背景轨道
- dotnet build 编译通过
- 模板保持圆角视觉风格（CornerRadius=4, RadiusX=4, RadiusY=4）

## Proof Level

- This slice proves: contract — 模板包含正确的命名元素，WPF ProgressBar 内部机制会自动处理宽度

## Integration Closure

修改 App.xaml 全局样式。所有使用 ModernProgressBarStyle 的 ProgressBar（当前只有 DownloadProgressWindow.xaml）会自动获得修复。无需额外接线。

## Verification

- Not provided.

## Tasks

- [x] **T01: 修复 ModernProgressBarStyle 模板添加 PART_Indicator** `est:15m`
  修复 App.xaml 中 ModernProgressBarStyle 的 ControlTemplate，添加 WPF ProgressBar 标准要求的 PART_Indicator 命名元素。当前模板只有 PART_Track 且 Fill 绑定了 Foreground（填充色），缺少 PART_Indicator 导致进度条填充区域不随 Value 增长。

根因分析：
- WPF ProgressBar 通过 TemplatePart 名称约定查找 PART_Track 和 PART_Indicator
- PART_Indicator 的宽度由 ProgressBar 内部根据 Value/Minimum/Maximum 比例自动设置
- 缺少 PART_Indicator 时，无论 Value 如何变化都无可见填充

修复方案（来自 M019-CONTEXT.md 架构决策）：
1. 在 ControlTemplate 内用 Grid 包裹两个 Rectangle
2. PART_Track 保持透明（Border 已有 Background 作为轨道背景色）
3. 新增 PART_Indicator，Fill 绑定 Foreground（PrimaryBrush 蓝色），HorizontalAlignment=Left
4. 两个 Rectangle 都保持 RadiusX/RadiusY=4 圆角风格
5. 外层 Border CornerRadius=4 保持不变
  - Files: `App.xaml`
  - Verify: dotnet build && grep -q 'PART_Indicator' App.xaml && grep -c 'PART_Track\|PART_Indicator' App.xaml

## Files Likely Touched

- App.xaml
