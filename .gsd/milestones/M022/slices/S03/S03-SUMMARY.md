---
id: S03
parent: M022
milestone: M022
provides:
  - ["docs/cross-platform-research/avalonia-research.md — Avalonia 文献调研", "docs/cross-platform-research/blazor-hybrid-research.md — Blazor Hybrid 文献调研", "docs/cross-platform-research/web-app-research.md — 纯 Web 应用文献调研", "docs/cross-platform-research/maui-research.md — MAUI 文献调研"]
requires:
  []
affects:
  - ["S05"]
key_files:
  - ["docs/cross-platform-research/avalonia-research.md", "docs/cross-platform-research/blazor-hybrid-research.md", "docs/cross-platform-research/web-app-research.md", "docs/cross-platform-research/maui-research.md"]
key_decisions:
  - (none)
patterns_established:
  - (none)
observability_surfaces:
  - none
drill_down_paths:
  - [".gsd/milestones/M022/slices/S03/tasks/T01-SUMMARY.md", ".gsd/milestones/M022/slices/S03/tasks/T02-SUMMARY.md", ".gsd/milestones/M022/slices/S03/tasks/T03-SUMMARY.md", ".gsd/milestones/M022/slices/S03/tasks/T04-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-05-04T16:50:13.296Z
blocker_discovered: false
---

# S03: 纯文献调研（Avalonia/Blazor Hybrid/Web/MAUI）

**完成 Avalonia、Blazor Hybrid、纯 Web 应用、.NET MAUI 四个跨平台方案的独立文献调研文档，均覆盖 11+ 个必需章节、3000+ 字、无 TBD/TODO 占位符**

## What Happened

本 slice 完成了四个跨平台方案的纯文献调研，每个方案产出一份独立的调研文档：

1. **Avalonia** (3130 字, 13 章节): 调研发现 Avalonia 是 WPF 跨平台迁移的最优方案——XAML 高度兼容、ViewModel/服务层直接复用、Velopack 无缝集成、性能接近 WPF、社区高度活跃（29K+ GitHub Stars），综合评分 4.3/5。

2. **Blazor Hybrid** (3003 字, 13 章节): Blazor Hybrid 提供了三种宿主选项（MAUI/WPF/Electron），服务层复用度高，但 WebView 渲染层引入额外抽象。Linux 依赖社区 WebKitGTK 方案，成熟度待验证，综合评分 3.7/5。

3. **纯 Web 应用** (3071 字, 13 章节): 技术栈最成熟但与 DocuFiller 核心需求（重度文件系统依赖）存在根本性不匹配。浏览器沙箱限制本地文件操作，推荐的本地后端服务模式可绕过但引入 HTTP 传输开销，综合评分 3.0/5。

4. **.NET MAUI** (3107 字, 12 章节): 技术架构合理（Handler 模式），服务层可高度复用，但 Microsoft 官方不支持 Linux 桌面，社区方案尚不成熟。XAML 控件差异大需完全重写 UI，综合评分 2.8/5。

四份文档均参考 electron-net-research.md 的格式和章节结构，保持本系列调研文档的一致性。

## Verification

所有四份文档通过质量验证：
- avalonia-research.md: 存在 ✅, 13 章节 (≥8) ✅, 无 TBD/TODO ✅, 3130 字 (≥3000) ✅
- blazor-hybrid-research.md: 存在 ✅, 13 章节 (≥8) ✅, 无 TBD/TODO ✅, 3003 字 (≥3000) ✅
- web-app-research.md: 存在 ✅, 13 章节 (≥8) ✅, 无 TBD/TODO ✅, 3071 字 (≥3000) ✅
- maui-research.md: 存在 ✅, 12 章节 (≥8) ✅, 无 TBD/TODO ✅, 3107 字 (≥3000) ✅

验证方法：gsd_exec 运行 bash 脚本检查文件存在性、## 章节数、TBD/TODO 关键词、字数统计。

## Requirements Advanced

- R063 — 完成四份方案调研文档，覆盖 Avalonia、Blazor Hybrid、纯 Web 应用、MAUI，每份均 ≥11 个维度、≥3000 字、无占位符

## Requirements Validated

- R063 — 四份文档均通过自动化质量验证：文件存在、≥8 章节、无 TBD/TODO、≥3000 字。综合评分从高到低：Avalonia 4.3/5 > Blazor Hybrid 3.7/5 > Web 3.0/5 > MAUI 2.8/5

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

- `docs/cross-platform-research/avalonia-research.md` — Avalonia UI 跨平台方案文献调研文档 (3130 字, 13 章节, 评分 4.3/5)
- `docs/cross-platform-research/blazor-hybrid-research.md` — Blazor Hybrid 跨平台方案文献调研文档 (3003 字, 13 章节, 评分 3.7/5)
- `docs/cross-platform-research/web-app-research.md` — 纯 Web 应用方案文献调研文档 (3071 字, 13 章节, 评分 3.0/5)
- `docs/cross-platform-research/maui-research.md` — .NET MAUI 跨平台方案文献调研文档 (3107 字, 12 章节, 评分 2.8/5)
