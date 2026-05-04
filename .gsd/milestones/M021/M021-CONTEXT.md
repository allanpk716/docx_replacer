# M021: 第二轮重构 — ViewModel 拆分 + 拖放 Behavior + R028 自动更新 + 文档同步

**Gathered:** 2026-05-04
**Status:** Ready for planning

## Project Description

DocuFiller 第二轮重构，聚焦 MainWindowViewModel 的全面拆分、清理 Tab 重复代码消除、拖放逻辑提取、服务接口补全、自动更新通知实现，以及文档同步和 CLAUDE.md 清理。

## Why This Milestone

M020 完成了第一轮代码质量清理（死代码、重复代码提取、CT.Mvvm 迁移、配置清理），但 MainWindowViewModel（1623 行）仍然是最大的单文件技术债——关键词替换、清理、更新三大职责混杂在一个类中。同时清理 Tab 和 CleanupViewModel 有两套几乎相同的实现。拖放逻辑占 code-behind 400 行且高度重复。R028（自动检查更新）从未被安排。

## User-Visible Outcome

### When this milestone is complete, the user can:

- 使用关键词替换 Tab 进行完整的模板填充流程（行为不变）
- 使用清理 Tab 进行文件清理（行为不变，底层代码统一）
- 应用启动后自动检查更新，状态栏显示通知（R028 新功能）
- 拖放文件到 TextBox 和 Border（行为不变，底层代码简化）

### Entry point / environment

- Entry point: DocuFiller.exe（GUI 双 Tab）或 DocuFiller.exe update（CLI）
- Environment: Windows 桌面

## Completion Class

- Contract complete means: 所有 XAML 绑定在拆分后正常工作，dotnet build 零错误，dotnet test 全部通过
- Integration complete means: GUI 启动→关键词替换 Tab 完整流程→清理 Tab 完整流程→更新状态显示，全部正常
- Operational complete means: 应用启动后 5 秒内自动检查更新（R028），成功/失败都不影响正常使用

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- GUI 启动后关键词替换 Tab 所有功能正常（模板选择、数据加载、预览、处理、取消、进度显示）
- 清理 Tab 添加文件→开始清理→显示结果完整流程正常
- 状态栏显示更新状态，启动 5 秒后自动检查（R028）
- MainWindowViewModel.cs 行数 ≤ 400 行
- CLAUDE.md 已不存在

## Architectural Decisions

### ViewModel 拆分策略

**Decision:** MainWindowViewModel 拆分为 FillViewModel（CT.Mvvm）+ UpdateStatusViewModel（CT.Mvvm）+ 协调器 MainWindowVM（保留手写 INPC）

**Rationale:** FillViewModel 承载关键词替换 Tab 全部业务逻辑（~700行），UpdateStatusViewModel 承载更新状态管理（~200行），MainWindowVM 降为纯协调器。CT.Mvvm 减少样板代码，与 M020 迁移的 DownloadProgressViewModel/UpdateSettingsViewModel 一致。

**Alternatives Considered:**
- 只拆最大的（FillViewModel），更新和清理留在 MainWindowVM — 减少工作量但 Coordinator 仍然臃肿
- 全部使用手写 INPC — 与 M020 建立的 CT.Mvvm 先例矛盾

### 清理 Tab 复用 CleanupViewModel

**Decision:** Tab 2 的 XAML DataContext 改为绑定 CleanupViewModel，删除 MainWindowVM 中 ~200 行重复清理代码

**Rationale:** 两套实现功能相同（文件列表、处理、进度），复用消除重复。CleanupViewModel 需扩展输出目录属性以匹配 Tab 2 需求。

**Alternatives Considered:**
- 新建独立 CleanupTabViewModel — 仍然两套实现
- 不拆，保持现状 — 重复代码持续存在

### DragDropBehavior AttachedProperty

**Decision:** 提取为自定义 DragDropBehavior，TextBox/Border 设附加属性即可支持拖放

**Rationale:** 15 个事件处理器中 12 个逻辑几乎相同（4 组 Preview 事件 × 3 目标）。Behavior 模式是 WPF AttachedProperty 标准做法，可复用于未来窗口。

**Alternatives Considered:**
- 移入 ViewModel — ViewModel 需处理 UI 概念（DragEventArgs），不纯粹
- 不动 — 400 行重复 code-behind 继续存在

### R028 实现方式

**Decision:** UpdateStatusViewModel 构造时启动延迟 5 秒的静默检查。失败静默，有新版本时状态栏显示视觉指示。

**Rationale:** 不阻塞 UI 启动，不打扰用户，只在有可操作信息时才通知。与手动"检查更新"按钮的显式反馈形成互补。

### CLAUDE.md 删除

**Decision:** 删除 CLAUDE.md，不再维护。产品需求文档和 README.md 作为唯一项目文档。

**Rationale:** CLAUDE.md 维护成本高且容易与代码脱节。用户决定不再投入维护。

## Error Handling Strategy

- ViewModel 拆分过程中的 DI 异常：fail-fast（应用启动崩溃），表明 DI 配置错误
- DragDropBehavior 拖放异常：静默忽略，文件访问异常在 ViewModel 层处理
- R028 自动检查更新失败：静默处理，状态栏保持默认状态，不打扰用户
- CleanupViewModel 合并冲突：统一到 CleanupViewModel 现有实现
- DocumentProcessorService 接口补全：编译期检查，零运行时风险

## Risks and Unknowns

- DragDropBehavior 能否完全消除 code-behind — TextBox 的 Preview 事件策略（M017 D042）需要 Behavior 内部处理
- CleanupViewModel 扩展输出目录后，CleanupWindow（独立窗口）的输出目录逻辑如何协调 — 两处使用可能需要不同默认值
- MainWindowVM → 子 VM 的 DataContext 切换可能遗漏某些绑定 — 需要逐一验证 XAML 绑定路径

## Existing Codebase / Prior Art

- `ViewModels/DownloadProgressViewModel.cs` — CT.Mvvm 迁移参考（M020/S04）
- `ViewModels/UpdateSettingsViewModel.cs` — CT.Mvvm 迁移参考（M020/S04）
- `DocuFiller/ViewModels/CleanupViewModel.cs` — 清理逻辑参考实现
- `Services/Interfaces/IDocumentProcessor.cs` — 已有接口定义
- `Services/FileService.cs` — ILogger 结构化日志参考（M020/S01）

## Relevant Requirements

- R060 — MainWindowViewModel 拆分
- R061 — 清理 Tab 复用 CleanupViewModel
- R062 — DragDropBehavior 提取
- R063 — CleanupViewModel CT.Mvvm 迁移
- R064 — IDocumentProcessor 接口补全
- R065 — DocumentCleanupService 日志补充
- R028 — 启动自动检查更新
- R066 — CLAUDE.md 删除
- R067 — 产品需求文档同步

## Scope

### In Scope

- MainWindowViewModel 全面拆分（FillViewModel + UpdateStatusViewModel + 协调器）
- 清理 Tab 复用 CleanupViewModel
- CleanupViewModel CT.Mvvm 迁移
- DragDropBehavior AttachedProperty 提取
- DocumentProcessorService 接口补全
- DocumentCleanupService ILogger 补充
- R028 启动自动检查更新 + 通知徽章
- CLAUDE.md 删除
- 产品需求文档 UI 描述同步更新

### Out of Scope / Non-Goals

- 其他 6 个服务的接口补全（CommentManager、ContentControlProcessor 等）
- MainWindowViewModel 自身迁移到 CT.Mvvm（保留手写 INPC）
- 新功能开发（R028 除外）
- E2E 测试新增

## Technical Constraints

- Windows 环境，PowerShell 替代 grep
- WPF BAML AssemblyVersion 必须保持 1.0.0.0
- CT.Mvvm 必须使用完全限定名避免与项目 ObservableObject.cs 冲突
- 每次修改后必须 `dotnet build` 确认编译通过

## Integration Points

- MainWindow.xaml XAML 绑定 — DataContext 切换到子 VM
- App.xaml.cs DI 注册 — 新增 FillViewModel、UpdateStatusViewModel 注册
- CleanupWindow.xaml — 共享 CleanupViewModel，需确认兼容性
- IUpdateService — R028 自动检查依赖现有更新服务

## Testing Requirements

- 新增 ViewModel 单元测试（属性变更通知、命令可用性）
- UpdateStatusViewModel 自动检查逻辑测试（延迟触发、失败静默）
- DragDropBehavior 如可行加单元测试，否则手动 UAT
- 不为纯搬移代码重复写测试
- 每个 slice 完成后 `dotnet test` 全部通过

## Acceptance Criteria

- S01: MainWindowVM ≤ 400 行，关键词替换 Tab 和更新状态功能全部正常
- S02: 清理 Tab 和独立窗口共用 CleanupViewModel，CT.Mvvm 源码生成
- S03: MainWindow.xaml.cs 拖放事件处理器 ≤ 3 个（Window 级别处理）
- S04: IDocumentProcessor 覆盖 DocumentProcessorService 公共 API；CleanupService 5 个 catch 块有 ILogger
- S05: 应用启动 5 秒后状态栏自动显示更新状态
- S06: CLAUDE.md 不存在；产品需求文档 UI 描述与实际代码一致

## Open Questions

- None — all layers confirmed
