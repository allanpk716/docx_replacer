---
phase: execute
phase_name: Execute
project: DocuFiller
generated: "2026-05-03T11:55:00Z"
counts:
  decisions: 2
  lessons: 3
  patterns: 3
  surprises: 1
missing_artifacts: []
---

# M019 LEARNINGS

### Decisions

- **D046 — ProgressBar 模板修复方式**: 在 ModernProgressBarStyle 模板中添加 PART_Indicator（WPF 标准命名），保持圆角视觉风格。选择标准 WPF TemplatePart 约定而非自定义动画。
  Source: S01-SUMMARY.md/What Happened
- **D047 — 图标生成方式**: 使用 Python + Pillow 程序化绘制图标（蓝色 Word 文档 + 绿色对勾），手动构建 ICO 二进制以包含多分辨率帧（Pillow ICO 保存器只写第一帧）。
  Source: S02-SUMMARY.md/What Happened

### Lessons

- **WPF ProgressBar 缺少 PART_Indicator 不会报错但永远不增长**: WPF ProgressBar 通过 TemplatePart 名称约定查找 PART_Indicator 并自动管理其宽度。模板只有 PART_Track 时，Value 变化无可见效果，也不抛异常，属于静默失败。
  Source: S01-SUMMARY.md/What Happened
- **Pillow ICO 保存器只写入第一帧**: 使用 Pillow 生成多分辨率 ICO 时，Image.save('file.ico') 只保留第一帧。需要按 ICO 规范手动构建二进制文件（ICONDIR + ICONDIRENTRY + PNG 帧）。
  Source: S02-SUMMARY.md/T01
- **csproj ApplicationIcon 和 WPF Resource 可共存**: 同一个 .ico 文件可通过 `<ApplicationIcon>` 嵌入 exe 资源，同时通过已有的 `<Resource Include>` 作为 WPF pack URI 引用，无需重复文件。
  Source: S02-SUMMARY.md/T01

### Patterns

- **WPF ProgressBar ControlTemplate 标准结构**: Border（轨道背景色 + CornerRadius）→ Grid → PART_Track（Fill=Transparent）+ PART_Indicator（Fill={TemplateBinding Foreground}, HorizontalAlignment=Left）。两个 Rectangle 保持相同圆角。
  Source: S01-SUMMARY.md/What Happened
- **WPF 窗口图标统一引用方式**: 所有窗口通过 pack URI `pack://application:,,,/Resources/app.ico` 引用图标，自定义标题栏使用 Image 控件 + RenderOptions.BitmapScalingMode="HighQuality"。
  Source: S02-SUMMARY.md/T02
- **程序化图标生成工作流**: Python/Pillow 绘制 → 多尺寸 PNG 导出 → 手动构建 ICO 二进制 → 保留生成脚本在项目根目录供未来重建。
  Source: S02-SUMMARY.md/What Happened

### Surprises

- **MainWindow.xaml 存在 XML 结构损坏**: 为 MainWindow 添加图标时发现重复的闭合标签导致 MC3000 构建错误。这是之前编辑遗留的问题，与本次改动无关但在添加 Icon 属性时暴露。
  Source: S02-SUMMARY.md/T02
