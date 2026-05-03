# S01: 修复进度条可见增长 — UAT

**Milestone:** M019
**Written:** 2026-05-03T11:43:42.919Z

# S01: 修复进度条可见增长 — UAT

**Milestone:** M019
**Written:** 2026-05-03

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: 此 slice 修改 XAML 模板（声明式 UI），dotnet build 通过即确认模板语法正确。运行时验证需要实际下载场景，但模板结构符合 WPF 标准约定可确保功能正确。

## Preconditions

- dotnet SDK 已安装
- 项目可成功构建

## Smoke Test

在 App.xaml 中搜索 ModernProgressBarStyle，确认 ControlTemplate 包含 PART_Indicator 和 PART_Track 两个命名元素，且 PART_Indicator 的 Fill 绑定了 Foreground、HorizontalAlignment 为 Left。

## Test Cases

### 1. 模板包含标准 ProgressBar 命名元素

1. 打开 App.xaml
2. 搜索 `ModernProgressBarStyle`
3. 在 ControlTemplate 内查找 `PART_Indicator` 和 `PART_Track`
4. **Expected:** 两个元素都存在，PART_Track Fill="Transparent"，PART_Indicator Fill="{TemplateBinding Foreground}" HorizontalAlignment="Left"

### 2. 构建验证

1. 运行 `dotnet build`
2. **Expected:** 0 错误，0 警告

### 3. 圆角风格保持一致

1. 检查 ControlTemplate 中的 Border 和 Rectangle 元素
2. **Expected:** Border CornerRadius=4，两个 Rectangle RadiusX=4 RadiusY=4

## Edge Cases

### 模板兼容性

1. 确认 TargetType="ProgressBar"
2. 确认使用了 TemplateBinding 而非硬编码颜色
3. **Expected:** 模板通过 TemplateBinding 绑定 Background 和 Foreground，适配不同主题色

## Failure Signals

- dotnet build 失败
- PART_Indicator 元素缺失
- PART_Track 或 PART_Indicator 的 Name 属性拼写错误
- 填充颜色硬编码而非使用 TemplateBinding

## Not Proven By This UAT

- 实际下载时进度条动画效果（需要真实下载场景）
- 不同 Minimum/Maximum 值下的填充比例
- 高 DPI 缩放下的渲染效果

## Notes for Tester

- 此修复修改的是全局样式 ModernProgressBarStyle，当前仅 DownloadProgressWindow 使用
- PART_Track 设为透明是正确的——轨道背景色由外层 Border 的 Background 属性提供
- WPF ProgressBar 内部机制会根据 Value/Minimum/Maximum 自动计算并设置 PART_Indicator 的宽度
