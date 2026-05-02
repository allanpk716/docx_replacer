# M012-li0ip5: 主界面布局紧凑化与拖放修复

**Gathered:** 2026-05-01
**Status:** Ready for planning

## Project Description

DocuFiller 主界面当前默认尺寸 1400x900，内容布局松散，在 1366x768 分辨率的笔记本上无法完整显示所有控件且没有滚动条。功能本身很简单（两个 Tab，每个只有几个输入框和按钮），完全可以通过紧凑布局在小屏幕上完整呈现。

## Why This Milestone

用户日常使用中遇到两个实际问题：
1. 小屏笔记本上看不全界面，操作受阻
2. 拖放文件/文件夹时经常失效，需要先点击窗口聚焦后才能拖放

## User-Visible Outcome

### When this milestone is complete, the user can:

- 在 1366x768 分辨率的笔记本上完整看到并操作所有功能，无需滚动
- 从资源管理器拖拽文件到 DocuFiller 窗口时，无论窗口是否聚焦，都能正常触发拖放
- 两个 Tab（关键词替换、审核清理）风格统一、布局紧凑

### Entry point / environment

- Entry point: GUI 模式（WPF MainWindow）
- Environment: Windows 桌面，目标最小分辨率 1366x768

## Completion Class

- Contract complete means: MainWindow.xaml 编译通过，控件布局在 1366x768 下完整可见
- Integration complete means: 拖放功能在窗口未聚焦状态下正常工作
- Operational complete means: 所有现有功能（关键词替换、审核清理、更新检查）不受影响

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- 启动应用，在 1366x768 分辨率下两个 Tab 的所有内容无需滚动即可完整显示
- 窗口最小化后恢复，从资源管理器拖拽 .docx 文件到路径文本框，拖放正常触发
- 执行一次完整的关键词替换流程（选模板→选数据→设输出→开始处理），功能不受影响
- 执行一次审核清理流程，功能不受影响

## Architectural Decisions

### 拖放区域从独立 Border 改为路径文本框内支持

**Decision:** 去掉独立的拖放 Border（70-80px 高），改为路径 TextBox 支持 AllowDrop

**Rationale:** 拖放区域是垂直空间最大消耗者（两个约 150px），改为单行路径栏可节省大量空间。TextBox 支持 AllowDrop 在 WPF 中是标准做法。

**Alternatives Considered:**
- 保留拖放 Border 但缩小高度 — 仍然占垂直空间，效果有限
- 完全去掉拖放只保留按钮 — 丧失拖放便利性，用户体验退化

### 去掉 GroupBox 改用简洁标签 + 分隔线

**Decision:** 三个 GroupBox（模板文件、数据文件、输出设置）替换为 TextBlock 标签 + Separator

**Rationale:** 每个 GroupBox 的 header 行约 30px + 内边距约 10px，三个共约 120px 额外空间。用标签 + 分隔线替代后只需约 20-30px，同时视觉更简洁。

**Alternatives Considered:**
- 保留 GroupBox 但压缩 — header 行仍然存在，节省有限
- 用 Expander — 增加交互复杂度，不必要

### 窗口尺寸从 1400x900 降至 900x550

**Decision:** 默认 Width=900 Height=550，MinWidth=800 MinHeight=500

**Rationale:** 内容紧凑化后不再需要大窗口。900x550 在 1366x768 下（减去任务栏约 40px 后可用 ~728px）充裕显示。

**Alternatives Considered:**
- 1100x600 — 仍然偏大，紧凑布局用不到
- 不改窗口尺寸 — 小屏幕上仍然看到大窗口

### 拖放焦点修复方案

**Decision:** 在 Window 级别处理 DragEnter/DragOver 事件确保 OLE 拖放管道激活，同时在 Activated 事件中强制重新注册拖放目标

**Rationale:** WPF 拖放依赖 COM OLE 拖放注册。当窗口不是前台窗口时，子控件的 AllowDrop 事件可能不触发。在 Window 级别处理可确保拖放管道始终激活。

---

> See `.gsd/DECISIONS.md` for the full append-only register of all project decisions.

## Error Handling Strategy

纯 UI 调整，不涉及业务逻辑错误处理变更。拖放修复确保在异常情况下（窗口切换、最小化恢复）拖放仍能正常工作。

## Risks and Unknowns

- TextBox 的 AllowDrop 与 IsReadOnly 组合可能有意外行为 — 需要实际测试确认拖放事件能正确触发
- WPF 拖放焦点问题的根因可能涉及更深的 OLE 层 — 如果 Window 级别处理不够，可能需要 Windows API interop

## Existing Codebase / Prior Art

- `MainWindow.xaml` — 当前布局，Height=900 Width=1400，三个 GroupBox + 两个拖放 Border
- `MainWindow.xaml.cs` — code-behind 拖放事件处理，绑定在 Border 控件上，需迁移到 TextBox
- `App.xaml` — 全局样式定义（GroupBoxStyle、PrimaryButton、TextBoxStyle 等），需同步调整字号和间距

## Relevant Requirements

- R050 — 主界面在 1366x768 下无需滚动完整显示
- R051 — 拖放区域紧凑化为单行路径栏
- R052 — 全局字号从 16px 降至 12-14px
- R053 — GroupBox 替换为简洁标签
- R054 — 窗口未聚焦时拖放正常工作
- R055 — 审核清理 Tab 同步紧凑化

## Scope

### In Scope

- MainWindow.xaml 布局紧凑化（两个 Tab）
- App.xaml 全局样式字号和间距调整
- MainWindow.xaml.cs 拖放事件从 Border 迁移到 TextBox
- 拖放焦点 bug 修复
- 窗口尺寸调整

### Out of Scope / Non-Goals

- 不改变任何业务逻辑
- 不改变 ViewModel 代码
- 不改变 CLI 模式
- 不改变 CleanupWindow 等子窗口（它们是弹窗，布局独立）
- 不做响应式布局（不是 web）

## Technical Constraints

- WPF XAML 布局，Windows only
- 保留现有的拖放视觉反馈效果（边框变色等）但适配到新的控件类型
- 保留现有的 code-behind 拖放事件模式，不引入第三方拖放库

## Integration Points

- MainWindow.xaml ↔ MainWindow.xaml.cs（拖放事件绑定）
- MainWindow.xaml ↔ App.xaml（样式引用）
- MainWindow.xaml ↔ MainWindowViewModel.cs（数据绑定，不改）

## Testing Requirements

- `dotnet build` 编译通过
- 手动验证 1366x768 下布局完整性
- 手动验证拖放在窗口未聚焦时正常工作
- 执行一次完整的替换流程确认功能不受影响

## Acceptance Criteria

### S01（布局紧凑化）

- 窗口在 1366x768 下两个 Tab 所有控件完整可见，无需滚动
- 拖放区域改为路径文本框，高度约 30px（替代原来的 70-80px Border）
- GroupBox 替换为标签 + 分隔线，视觉清晰
- 字号降至 12-14px 范围
- 审核清理 Tab 同步紧凑化

### S02（拖放修复）

- 窗口未聚焦时从资源管理器拖拽文件到路径文本框，拖放正常触发
- 拖拽时有视觉反馈（文本框边框变色）
- 拖放后文件路径正确显示
- 所有三种拖放场景（模板文件/文件夹、数据文件、清理文件）均正常

## Open Questions

- 无 — 讨论阶段已解决所有关键决策
