---
id: T03
parent: S03
milestone: M022
key_files:
  - docs/cross-platform-research/web-app-research.md
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-05-04T16:43:06.058Z
blocker_discovered: false
---

# T03: 撰写纯 Web 应用方案文献调研文档，覆盖技术架构、文件系统访问限制、PWA 能力、部署模式等 11 个维度，3071 字

**撰写纯 Web 应用方案文献调研文档，覆盖技术架构、文件系统访问限制、PWA 能力、部署模式等 11 个维度，3071 字**

## What Happened

通过 Web 搜索收集了 ASP.NET Core SPA/PWA、File System Access API、Blazor WebAssembly 性能、Tauri 桌面包装等方面的技术资料，参考已有调研文档（Electron.NET、Avalonia、Blazor Hybrid）的格式和深度，撰写了纯 Web 应用方案的完整调研文档。

文档覆盖了任务要求的所有 11 个章节：技术概述（ASP.NET Core 后端 + React/Vue/Blazor WASM 前端选型、PWA 模式、Tauri 包装）、与 DocuFiller 的适配性分析（后端 100% 复用、UI 完全重写、文件系统访问核心挑战）、跨平台支持状态（浏览器即跨平台、PWA 离线能力局限）、前端生态与依赖、.NET 8 兼容性、部署模式（本地后端服务模式推荐）、社区活跃度与成熟度、性能特征（网络开销、大文件处理瓶颈、Blazor WASM 性能）、优缺点 SWOT 分析、成熟度评估（1-5 分评分 + TRL 等级）、调研日期与信息来源。

调研的核心发现：纯 Web 方案虽然技术栈最成熟，但与 DocuFiller 的核心需求（重度文件系统依赖）存在根本性不匹配。浏览器沙箱严重限制了本地文件操作，即使使用 File System Access API 也仅 Chromium 浏览器支持且功能有限。推荐的本地后端服务模式（ASP.NET Core 本地运行 + 浏览器前端）可绕过此限制，但引入了 HTTP 传输开销和安装复杂度。综合评分 3.0/5，在四个方案中排名最低。

## Verification

运行验证命令检查文档质量：文件存在、13 个二级标题（≥8 通过）、无 TBD/TODO 标记、字数 3071（≥3000 通过）。所有验证检查均通过。

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `bash -c 'FILE="docs/cross-platform-research/web-app-research.md" && test -f "$FILE" && grep -c "^## " "$FILE" | grep -q "^[8-9]\|[1-9][0-9]$" && ! grep -q "TBD\|TODO" "$FILE" && wc -w "$FILE" | awk "{if(\$1 >= 3000) exit 0; else exit 1}"'` | 0 | ✅ pass | 500ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `docs/cross-platform-research/web-app-research.md`
