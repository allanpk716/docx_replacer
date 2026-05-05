# M022: 跨平台技术调研

**Gathered:** 2026-05-04
**Status:** Ready for planning

## Project Description

DocuFiller 当前是一个 .NET 8 + WPF 桌面应用，深度绑定 Windows 平台。本次调研的目标是积累跨平台（Linux/macOS）技术知识，为未来可能的迁移决策提供依据。产出是调研文档和 PoC 代码，不修改现有项目。

## Why This Milestone

用户提出"如果这个项目要在 Linux 和 macOS 上运行，应该怎么做？"的问题。需要系统性地调研所有可能的跨平台 UI 替代方案，尤其关注前端技术开发技术是否能替代 WPF。

## User-Visible Outcome

### When this milestone is complete, the user can:

- 阅读 `docs/cross-platform-research/` 下的完整调研报告
- 运行两个 PoC 项目（Electron.NET、Tauri + .NET sidecar）验证技术可行性
- 基于对比评估文档做出技术选型决策

### Entry point / environment

- Entry point: 文档阅读 + PoC 项目编译运行
- Environment: Windows 开发环境，编译验证为主
- Live dependencies involved: 无

## Completion Class

- Contract complete means: 所有调研文档写入 `docs/cross-platform-research/`，PoC 项目在 Windows 上编译通过
- Integration complete means: PoC 能运行并展示迷你 DocuFiller 功能（文件选择→处理→进度条）
- Operational complete means: 不适用（纯调研）

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- 两个 PoC 项目能编译并运行
- 所有调研文档覆盖约定的课题
- 最终对比评估文档包含所有 6 个方案

## Architectural Decisions

### 调研范围

**Decision:** 6 个 UI 方案 + 基础设施课题。其中 Electron.NET 和 Tauri + .NET sidecar 写 PoC，其余 4 个纯文献调研。

**Rationale:** 用户明确指定 Electron.NET 和 Tauri 为 PoC 目标，对 Web 前端技术替换 UI 有兴趣。

**Alternatives Considered:**
- 全部写 PoC → 工作量过大，且 Avalonia/MAUI 文献调研已经足够
- 只做文献调研 → 用户要求实际测试代码验证

### PoC 功能范围

**Decision:** 迷你 DocuFiller（文件选择→处理→进度条），模拟核心用户操作流程。

**Rationale:** 覆盖 DocuFiller 最核心的交互模式，能暴露文件对话框、进度汇报、前后端通信等关键集成点。

**Alternatives Considered:**
- Hello World 级别 → 太简单，无法暴露真实集成问题
- 完整 DocuFiller 克隆 → 工作量不属于调研范畴

### 调研环境约束

**Decision:** Windows 上编译验证 + 文献调研，不做 Linux/macOS 实机测试。

**Rationale:** 调研目标是知识积累，不是生产迁移。Windows 编译通过 + API 可用足以判断方案可行性。

## Error Handling Strategy

不适用——纯调研里程碑。

## Risks and Unknowns

- Tauri 的 .NET sidecar 集成成熟度不明，官方主要面向 Rust/JS 生态，可能需要自行设计 IPC 方案
- Electron.NET 项目活跃度需要确认（历史上有维护中断的情况）
- Windows 上无法验证 Linux/macOS 特定的运行时行为

## Existing Codebase / Prior Art

- `DocuFiller.csproj` — `net8.0-windows` + `UseWPF=true`，跨平台迁移的起点
- `Services/` — 零 WPF 依赖，纯 .NET 跨平台兼容
- `Cli/` — 几乎零 WPF 依赖（仅 `ConsoleHelper.cs` 有 P/Invoke）
- `ViewModels/` — 深度 WPF 耦合（OpenFileDialog、MessageBox、Dispatcher）
- `update-server/` — Go 编写，已跨平台
- Velopack — 已支持 Windows/macOS/Linux

## Relevant Requirements

- 本里程碑新增调研类 requirements（R061-R067）

## Scope

### In Scope

- Electron.NET PoC + 调研文档
- Tauri + .NET sidecar PoC + 调研文档
- Avalonia、Blazor Hybrid、纯 Web、MAUI 文献调研
- Velopack 跨平台能力调研
- 核心依赖库（OpenXml、EPPlus）跨平台兼容性调研
- 平台差异处理（文件对话框、拖放、路径）调研
- 打包分发方案（macOS dmg/notarization、Linux deb/AppImage）调研
- 总结对比评估文档

### Out of Scope / Non-Goals

- 任何对现有 DocuFiller 项目的代码修改
- Linux/macOS 实机测试
- 实际的迁移实施

## Technical Constraints

- PoC 代码必须在独立目录，不混入现有项目
- 调研文档写入 `docs/cross-platform-research/`，标注调研日期
- Windows 环境编译验证

## Integration Points

- 无外部集成。调研参考现有代码库结构和依赖。

## Testing Requirements

PoC 项目需在 Windows 上编译通过并能运行。不需要单元测试。

## Acceptance Criteria

- 每个调研文档包含：技术概述、与 DocuFiller 的适配性分析、优缺点、成熟度评估
- PoC 项目实现迷你 DocuFiller 功能
- 最终对比评估文档包含所有 6 个方案的横向对比

## Open Questions

- Tauri .NET sidecar 的 IPC 机制需要 PoC 验证确认
