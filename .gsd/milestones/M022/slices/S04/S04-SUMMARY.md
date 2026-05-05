---
id: S04
parent: M022
milestone: M022
provides:
  - ["docs/cross-platform-research/velopack-cross-platform.md — Velopack 三平台更新能力调研", "docs/cross-platform-research/core-dependencies-compatibility.md — 16个 NuGet 依赖跨平台兼容性调研", "docs/cross-platform-research/platform-differences.md — 6大平台差异点及代码修改清单", "docs/cross-platform-research/packaging-distribution.md — macOS/Linux 打包分发方案及 CI/CD 设计"]
requires:
  []
affects:
  - ["S05"]
key_files:
  - ["docs/cross-platform-research/velopack-cross-platform.md", "docs/cross-platform-research/core-dependencies-compatibility.md", "docs/cross-platform-research/platform-differences.md", "docs/cross-platform-research/packaging-distribution.md"]
key_decisions:
  - ["Velopack 可作为跨平台统一更新框架，Linux 端需 NFPM 补充 deb/rpm 分发", "DocuFiller 全部核心 NuGet 依赖均为纯托管实现，支持 net8.0 跨平台运行", "DocuFiller 不使用 Windows 注册表，路径处理已天然跨平台", "WPF 文件对话框（5处）和拖放（FileDragDrop.cs）是迁移主要工作量", "推荐 AppImage 作为 Linux 首选打包格式，macOS 使用 create-dmg + Velopack", "建议使用自包含发布（--self-contained true），Linux 产物在 Ubuntu 18.04 容器中构建"]
patterns_established:
  - ["调研文档格式规范：中文撰写、日期标注、目录、≥12个编号章节、信息来源列表、≥3000字", "每个调研课题包含：技术概述、各平台分析、工具链对比、局限性讨论、DocuFiller 建议"]
observability_surfaces:
  - none
drill_down_paths:
  - [".gsd/milestones/M022/slices/S04/tasks/T01-SUMMARY.md", ".gsd/milestones/M022/slices/S04/tasks/T02-SUMMARY.md", ".gsd/milestones/M022/slices/S04/tasks/T03-SUMMARY.md", ".gsd/milestones/M022/slices/S04/tasks/T04-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-05-04T17:20:48.548Z
blocker_discovered: false
---

# S04: 通用课题调研（Velopack/核心库/平台差异/打包分发）

**完成四份基础设施调研文档：Velopack 跨平台能力、16个 NuGet 依赖兼容性、6大平台差异点、macOS/Linux 打包分发方案，确认核心业务库均可直接跨平台运行**

## What Happened

S04 完成了 DocuFiller 跨平台迁移的四个基础设施课题调研，产出四份独立文档：

**T01 — Velopack 跨平台能力（velopack-cross-platform.md，13章节，30KB）**
调研了 Velopack 在 Windows/macOS/Linux 三平台的自动更新能力。核心结论：Velopack 可作为统一更新框架，Windows 最成熟（已在用），macOS 基本可用（需 Apple Developer 账号 $99/年），Linux 仅支持 AppImage（deb/rpm 需 NFPM 补充）。增量更新（Zstandard delta）三平台通用。DocuFiller 现有 Velopack SDK 代码（UpdateManager、SimpleWebSource、GithubSource）跨平台无需修改。

**T02 — 核心依赖库兼容性（core-dependencies-compatibility.md，13章节，32KB）**
对 DocuFiller 全部 16 个 NuGet 依赖进行了跨平台兼容性验证。核心发现：DocumentFormat.OpenXml 3.0.1 和 EPPlus 7.5.2 均为纯 C# 托管实现，全平台兼容，无 COM 互操作或原生库依赖。Services/ 层 15/17 个服务文件零 Windows 依赖。唯一需适配的 3 处：ConsoleHelper（kernel32.dll）、ProgressReporterService（WPF Dispatcher）、App.xaml.cs（System.Configuration.ConfigurationManager）。

**T03 — 平台差异处理（platform-differences.md，13章节，30KB）**
通过全量源码扫描分析了 6 大差异领域：文件对话框（5 处 WPF 调用需重写）、拖放（FileDragDrop.cs 需按框架迁移）、路径处理（已天然跨平台，无需修改）、文件系统权限（XDG 规范对比 Windows ACL）、注册表（DocuFiller 未使用，无障碍）、进程管理（Process.Start 已跨平台可用）。产出了完整的平台特定代码点清单（10 个必须修改项 + 3 个建议修改项）。

**T04 — 打包分发方案（packaging-distribution.md，14章节，42KB）**
覆盖了 macOS（.app bundle、DMG、代码签名、公证、Homebrew Cask）和 Linux（AppImage 首选、deb 次选、rpm 第三、Flatpak/Snap）的完整打包分发方案。设计了基于 GitHub Actions matrix 的多平台并行构建 CI/CD 流水线。推荐使用自包含发布（--self-contained true），Linux 产物在 Ubuntu 18.04 容器中构建。

四份文档均与 S03 产出的格式保持一致：中文撰写、日期标注、目录、编号章节、信息来源。

## Verification

验证所有四份交付文档：

1. **文件存在性**：四份文件均存在于 docs/cross-platform-research/ 目录
   - velopack-cross-platform.md (30,638 bytes)
   - core-dependencies-compatibility.md (32,182 bytes)
   - platform-differences.md (30,316 bytes)
   - packaging-distribution.md (41,613 bytes)

2. **章节结构**：每份文档包含 ≥8 个 ## 级标题
   - velopack-cross-platform.md: 13 章节
   - core-dependencies-compatibility.md: 13 章节
   - platform-differences.md: 13 章节
   - packaging-distribution.md: 14 章节

3. **内容完整性**：所有文档零 TBD/TODO 标记

4. **任务验证**：所有 4 个任务（T01-T04）验证通过，task summary 已确认

5. **要求覆盖**：R064 已更新为 validated 状态，四份文档覆盖 Velopack 跨平台能力、核心依赖库兼容性、平台差异处理、打包分发方案全部要求

注意：原始验证命令使用 Unix 工具（test、grep）在 Windows CMD 下不可用，实际验证在 bash shell 中执行确认全部通过。

## Requirements Advanced

None.

## Requirements Validated

- R064 — 四份调研文档产出（velopack-cross-platform.md 13章节30KB、core-dependencies-compatibility.md 13章节32KB、platform-differences.md 13章节30KB、packaging-distribution.md 14章节42KB），覆盖 Velopack 跨平台能力、16个 NuGet 依赖兼容性、6大平台差异点、macOS/Linux 打包分发方案

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

- `docs/cross-platform-research/velopack-cross-platform.md` — Velopack 跨平台能力调研报告（13章节，30KB）
- `docs/cross-platform-research/core-dependencies-compatibility.md` — 核心依赖库跨平台兼容性调研报告（13章节，32KB）
- `docs/cross-platform/research/platform-differences.md` — 平台差异处理调研报告（13章节，30KB）
- `docs/cross-platform-research/packaging-distribution.md` — 打包分发方案调研报告（14章节，42KB）
