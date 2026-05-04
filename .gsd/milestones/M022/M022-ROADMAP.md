# M022: 跨平台技术调研

**Vision:** 系统性调研 DocuFiller 跨平台（Linux/macOS）迁移的所有可行技术方案。产出：2 个可运行的 PoC 项目（Electron.NET、Tauri + .NET sidecar）、4 个方案的文献调研文档、基础设施兼容性调研、以及最终的方案对比评估。所有调研文档写入 docs/cross-platform-research/，PoC 代码独立存放。

## Success Criteria

- 两个 PoC 项目（Electron.NET、Tauri）在 Windows 上编译通过并能运行迷你 DocuFiller 功能
- docs/cross-platform-research/ 下有完整的调研文档覆盖所有 6 个方案和基础设施课题
- 最终对比评估文档包含所有方案的横向对比
- PoC 代码在独立目录，不修改现有 DocuFiller 项目任何文件

## Slices

- [ ] **S01: Electron.NET PoC + 调研文档** `risk:high` `depends:[]`
  > After this: 能编译运行的 Electron.NET 迷你 DocuFiller，附完整调研报告

- [ ] **S02: Tauri + .NET sidecar PoC + 调研文档** `risk:high` `depends:[]`
  > After this: 能编译运行的 Tauri + .NET sidecar 迷你 DocuFiller，附完整调研报告

- [ ] **S03: 纯文献调研（Avalonia/Blazor/Web/MAUI）** `risk:low` `depends:[]`
  > After this: 四份独立的方案调研文档

- [ ] **S04: 通用课题调研（Velopack/核心库/平台差异/打包分发）** `risk:low` `depends:[]`
  > After this: 四份基础设施调研文档

- [ ] **S05: 总结与对比评估** `risk:low` `depends:[S01,S02,S03,S04]`
  > After this: 完整的跨平台方案对比评估文档

## Boundary Map

### S01 → S05
Produces:
  docs/cross-platform-research/electron-net-research.md — Electron.NET 完整调研报告
  poc/electron-net-docufiller/ — 可运行的 Electron.NET PoC 项目

Consumes: nothing (第一个 PoC slice)

### S02 → S05
Produces:
  docs/cross-platform-research/tauri-dotnet-research.md — Tauri + .NET sidecar 完整调研报告
  poc/tauri-docufiller/ — 可运行的 Tauri PoC 项目

Consumes: nothing (第二个 PoC slice)

### S03 → S05
Produces:
  docs/cross-platform-research/avalonia-research.md — Avalonia 文献调研
  docs/cross-platform-research/blazor-hybrid-research.md — Blazor Hybrid 文献调研
  docs/cross-platform-research/web-app-research.md — 纯 Web 应用文献调研
  docs/cross-platform-research/maui-research.md — MAUI 文献调研

Consumes: nothing (纯文献调研)

### S04 → S05
Produces:
  docs/cross-platform-research/velopack-cross-platform.md — Velopack 跨平台能力调研
  docs/cross-platform-research/core-dependencies-compatibility.md — 核心依赖库兼容性调研
  docs/cross-platform-research/platform-differences.md — 平台差异处理调研
  docs/cross-platform-research/packaging-distribution.md — 打包分发方案调研

Consumes: nothing (基础设施调研)

### S05 (最终评估)
Produces:
  docs/cross-platform-research/comparison-and-recommendation.md — 全方案对比评估与推荐

Consumes from S01: Electron.NET 调研报告和 PoC 发现
Consumes from S02: Tauri 调研报告和 PoC 发现
Consumes from S03: 四个方案的文献调研结果
Consumes from S04: 基础设施兼容性调研结果
