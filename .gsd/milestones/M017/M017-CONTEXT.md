# M017: 修复 TextBox 拖放被拦截

**Gathered:** 2026-05-03
**Status:** Ready for planning

## Project Description

DocuFiller 主界面有三个拖放区域：模板文件 TextBox、数据文件 TextBox、清理功能 Border。清理区域的 Border 拖放正常，但两个 TextBox 拖入文件时鼠标显示"禁止"图标，拖放完全不可用。

## Why This Milestone

用户已经在界面看到"可将 .docx 文件或文件夹拖放到上方文本框"的提示文字，但实际操作时拖放不可用。这是功能性 bug，直接影响用户对应用的信任。

## Root Cause

WPF TextBox 有内置的文本拖放处理（文本选择、拖拽选择），即使 `IsReadOnly="True"` + `AllowDrop="True"` 也会在冒泡阶段拦截外部文件拖放事件。清理区域用的 Border 没有内置拖放行为，所以正常工作。

**修复方案：** 将 TextBox 上的 `Drop`/`DragOver`/`DragEnter`/`DragLeave` 冒泡事件改为 `PreviewDrop`/`PreviewDragOver`/`PreviewDragEnter`/`PreviewDragLeave` 隧道事件。隧道事件从窗口向下传递，先于 TextBox 内部处理触发，在 handler 中设置 `e.Handled = true` 即可阻止 TextBox 拦截。

## User-Visible Outcome

### When this milestone is complete, the user can:

- 从资源管理器拖 .docx 文件到模板 TextBox，显示蓝色高亮并接受
- 从资源管理器拖文件夹到模板 TextBox，显示蓝色高亮并接受
- 从资源管理器拖 .xlsx 文件到数据 TextBox，显示蓝色高亮并接受
- 清理区域的拖放行为不受影响，保持正常

### Entry point / environment

- Entry point: GUI 主窗口，关键词替换 Tab
- Environment: Windows 桌面，WPF 应用

## Completion Class

- Contract complete means: 模板 TextBox 和数据 TextBox 均能接受文件拖放，拖入时显示高亮，放下后路径正确填入
- Integration complete means: 拖放后触发的后续操作（模板验证、数据预览）正常工作
- Operational complete means: 无

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- 拖 .docx 到模板 TextBox → 路径填入 + 验证信息显示
- 拖 .xlsx 到数据 TextBox → 路径填入 + 数据预览触发
- 拖非匹配文件到 TextBox → 显示文件类型错误提示

## Architectural Decisions

### 拖放事件路由策略

**Decision:** 从冒泡事件切换到 Preview 隧道事件

**Rationale:** TextBox 内置拖放处理在冒泡阶段拦截事件。隧道事件先于内置处理触发，`e.Handled = true` 阻止 TextBox 拦截。

**Alternatives Considered:**
- 用 Border 覆盖 TextBox（像清理区域那样）— 需要重构 XAML 布局，改动大
- 自定义 TextBox 子类重写 OnDragOver — 过度工程

## Error Handling Strategy

保持现有的错误处理不变：非匹配文件类型时弹出 MessageBox 提示，异常时弹出错误对话框。

## Risks and Unknowns

- 无重大风险。Preview 事件是 WPF 标准机制，代码端签名不变。

## Existing Codebase / Prior Art

- `MainWindow.xaml` — 两个 TextBox 的 DragOver/Drop 绑定需改为 Preview 版本
- `MainWindow.xaml.cs` — 事件处理器方法签名不变（DragEventHandler/DragEventArgs），方法名需与 XAML 同步更新
- `CleanupDropZoneBorder_*` — 清理区域使用冒泡事件，Border 无内置拦截，不需要改

## Relevant Requirements

- R059 — 修复 TextBox 拖放被拦截（primary）
- R058 — 拖放提示文字（已由 M016 完成，本里程碑确保提示对应的实际功能可用）

## Scope

### In Scope

- 将模板 TextBox 和数据 TextBox 的拖放事件改为 Preview 隧道事件
- 代码端事件处理器方法名同步更新
- 验证拖放功能正常工作

### Out of Scope / Non-Goals

- 清理区域拖放（已正常，不改动）
- 拖放视觉效果调整
- 新增拖放功能

## Technical Constraints

- WindowChrome + WindowStyle=None 环境下测试
- 不得影响清理区域的拖放行为

## Testing Requirements

- 编译通过（dotnet build）
- 手动验证：拖 .docx/.xlsx 到 TextBox
- 手动验证：拖非匹配文件到 TextBox 显示错误提示
- 已有测试不回归

## Acceptance Criteria

- 模板 TextBox 拖入 .docx 文件：蓝色高亮 + 路径填入 + 验证信息
- 模板 TextBox 拖入文件夹：蓝色高亮 + 文件夹路径处理
- 数据 TextBox 拖入 .xlsx 文件：蓝色高亮 + 路径填入 + 数据预览
- 拖入非匹配文件：提示文件类型错误
- 清理区域拖放不受影响
- dotnet build 无错误

## Open Questions

- 无
