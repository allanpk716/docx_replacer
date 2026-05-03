# M019: 进度条可见增长修复 + 应用图标

**Gathered:** 2026-05-03
**Status:** Ready for planning

## Project Description

修复下载进度窗口的 ProgressBar 不显示增长动画的问题，并为 DocuFiller 生成和_apply_应用图标。

## Why This Milestone

两个独立的视觉质量问题：进度条不动是功能性缺陷（用户无法直观感知进度），缺少图标是产品完整度缺陷。都是小范围修改，合并在一个里程碑中完成。

## User-Visible Outcome

### When this milestone is complete, the user can:

- 看到下载进度条的填充区域从左到右平滑增长
- 在窗口标题栏、任务栏、子窗口和 exe 文件上看到 DocuFiller 专属图标

### Entry point / environment

- Entry point: GUI 模式（启动应用）+ 触发更新下载
- Environment: Windows 桌面

## Completion Class

- Contract complete means: ProgressBar 模板包含 PART_Indicator 并正确响应 Value 变化；.ico 文件存在且被 csproj 和窗口引用
- Integration complete means: dotnet build 通过，运行时进度条可见增长，图标在所有位置正确显示
- Operational complete means: none（纯 UI 修改，无运行时生命周期影响）

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- 构建通过（dotnet build 无错误）
- ProgressBar 填充随值变化可见增长（代码审查确认 PART_Indicator 存在且绑定正确）
- 图标文件存在且被正确引用（csproj ApplicationIcon、窗口 Icon 属性、标题栏 emoji 替换）

## Architectural Decisions

### ProgressBar 修复方式

**Decision:** 在 ModernProgressBarStyle 模板中添加 PART_Indicator（标准 WPF 命名），保持圆角视觉风格

**Rationale:** WPF ProgressBar 通过名为 PART_Indicator 的元素显示填充进度。当前模板缺少此元素，导致填充不可见。添加它是标准修复方式，不需要自定义动画或 ValueChanged 事件处理。

**Alternatives Considered:**
- 自定义 ValueChanged + Width 绑定 — 不必要地复杂，绕过标准机制
- 使用第三方进度条控件 — 引入新依赖不值得

### 图标生成方式

**Decision:** 用 Python + Pillow 程序化绘制图标（文档页面 + 填充/箭头意象），导出多尺寸 .ico

**Rationale:** 不依赖外部设计工具或在线服务，可重复生成，脚本可纳入项目。DocuFiller 的定位（文档填充）有明确的视觉隐喻可用。

**Alternatives Considered:**
- 在线图标生成服务 — 需要网络，结果不可控
- SVG 转换 — .NET WPF 不原生支持 SVG，需要额外处理
- 手动绘制位图 — 不可重复，难以迭代

## Error Handling Strategy

图标缺失不会导致运行时崩溃（WPF 容错处理：窗口无图标显示默认图标）。构建时 csproj 的 `<ApplicationIcon>` 会在文件不存在时报错，提供编译期保障。

## Risks and Unknowns

- Pillow 生成的图标在小尺寸（16x16）下可能不够清晰 — 需要实际查看效果

## Existing Codebase / Prior Art

- `App.xaml` — 包含 `ModernProgressBarStyle` 定义，是修复目标
- `DocuFiller/Views/DownloadProgressWindow.xaml` — 使用 ProgressBar 的下载窗口
- `MainWindow.xaml` — 标题栏使用 📄 emoji，需要替换为图标引用
- `DocuFiller.csproj` — 需添加 `<ApplicationIcon>` 和 `<Resource>` 条目
- 项目目前无 `Resources/` 目录，无任何图片资源文件

## Relevant Requirements

- R046 — 进度条视觉增长反馈（M019/S01）
- R047 — 应用图标（M019/S02）

## Scope

### In Scope

- 修复 ModernProgressBarStyle 模板
- 生成 .ico 图标文件
- 应用图标到所有窗口和 exe

### Out of Scope / Non-Goals

- 其他 UI 改进或重构
- 重新设计窗口布局
- 新功能开发

## Technical Constraints

- WPF ProgressBar 标准模板约定：PART_Track + PART_Indicator
- .ico 文件需包含 16/32/48/256px 尺寸
- 图标生成需要 Python + Pillow 环境
- AssemblyVersion 固定为 1.0.0.0（不可改变）
- csproj 无 `<ApplicationIcon>` 现有条目，需新增

## Integration Points

- `App.xaml` 全局样式 — ProgressBar 样式修改影响所有使用该样式的 ProgressBar
- csproj 构建流程 — `<ApplicationIcon>` 影响编译输出

## Testing Requirements

- `dotnet build` 编译通过
- 代码审查确认模板结构正确（PART_Indicator 存在）
- 确认 .ico 文件包含所需尺寸

## Acceptance Criteria

### S01: 进度条修复
- ModernProgressBarStyle 模板包含 PART_Indicator 元素
- ProgressBar 的 Value 属性变化时，填充区域可见地从左向右增长

### S02: 应用图标
- Resources/ 目录下存在 .ico 文件
- csproj 包含 `<ApplicationIcon>` 引用
- 所有窗口（MainWindow、DownloadProgressWindow、UpdateSettingsWindow、CleanupWindow）设置 Icon 属性
- 标题栏的 📄 emoji 替换为 Image 控件引用图标

## Open Questions

- None
