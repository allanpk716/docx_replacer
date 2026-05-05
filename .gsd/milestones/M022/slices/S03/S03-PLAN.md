# S03: 纯文献调研（Avalonia/Blazor/Web/MAUI）

**Goal:** 为 Avalonia、Blazor Hybrid、纯 Web 应用、.NET MAUI 四个跨平台方案各撰写一份独立的文献调研文档，覆盖技术概述、与 DocuFiller 的适配性分析、跨平台支持状态、打包分发、社区活跃度、性能特征、优缺点和成熟度评估，供 S05 总结评估使用。
**Demo:** 四份独立的方案调研文档

## Must-Haves

- `docs/cross-platform-research/avalonia-research.md` 包含 ≥8 个章节，覆盖技术概述、DocuFiller 适配性、跨平台支持、优缺点、成熟度评估
- `docs/cross-platform-research/blazor-hybrid-research.md` 同上
- `docs/cross-platform-research/web-app-research.md` 同上
- `docs/cross-platform-research/maui-research.md` 同上
- 四份文档均无 TBD/TODO 占位符，均标注调研日期和信息来源

## Verification

- Run the task and slice verification checks for this slice.

## Tasks

- [ ] **T01: 撰写 Avalonia 跨平台方案文献调研文档** `est:45m`
  通过 Web 搜索和文档阅读，调研 Avalonia UI 框架的技术特性、跨平台支持、与 DocuFiller 的适配性，并撰写完整调研文档。
  - Files: `docs/cross-platform-research/avalonia-research.md`, `docs/cross-platform-research/electron-net-research.md`
  - Verify: bash -c 'FILE="docs/cross-platform-research/avalonia-research.md" && test -f "$FILE" && grep -c "^## " "$FILE" | grep -q "^[8-9]\|[1-9][0-9]$" && ! grep -q "TBD\|TODO" "$FILE" && wc -w "$FILE" | awk "{if(\$1 >= 3000) exit 0; else exit 1}"'

- [ ] **T02: 撰写 Blazor Hybrid 跨平台方案文献调研文档** `est:45m`
  通过 Web 搜索和文档阅读，调研 Blazor Hybrid（Blazor WebView + MAUI/Electron/WPF 宿主）的技术特性、跨平台支持、与 DocuFiller 的适配性，并撰写完整调研文档。
  - Files: `docs/cross-platform-research/blazor-hybrid-research.md`, `docs/cross-platform-research/electron-net-research.md`
  - Verify: bash -c 'FILE="docs/cross-platform-research/blazor-hybrid-research.md" && test -f "$FILE" && grep -c "^## " "$FILE" | grep -q "^[8-9]\|[1-9][0-9]$" && ! grep -q "TBD\|TODO" "$FILE" && wc -w "$FILE" | awk "{if(\$1 >= 3000) exit 0; else exit 1}"'

- [ ] **T03: 撰写纯 Web 应用方案文献调研文档** `est:45m`
  通过 Web 搜索和文档阅读，调研纯 Web 应用方案（ASP.NET Core 后端 + SPA/PWA 前端）作为 DocuFiller 跨平台方案的技术特性、可行性，并撰写完整调研文档。
  - Files: `docs/cross-platform-research/web-app-research.md`, `docs/cross-platform-research/electron-net-research.md`
  - Verify: bash -c 'FILE="docs/cross-platform-research/web-app-research.md" && test -f "$FILE" && grep -c "^## " "$FILE" | grep -q "^[8-9]\|[1-9][0-9]$" && ! grep -q "TBD\|TODO" "$FILE" && wc -w "$FILE" | awk "{if(\$1 >= 3000) exit 0; else exit 1}"'

- [ ] **T04: 撰写 .NET MAUI 跨平台方案文献调研文档** `est:45m`
  通过 Web 搜索和文档阅读，调研 .NET MAUI 的技术特性、跨平台支持（特别是 Linux/macOS 桌面支持）、与 DocuFiller 的适配性，并撰写完整调研文档。
  - Files: `docs/cross-platform-research/maui-research.md`, `docs/cross-platform-research/electron-net-research.md`
  - Verify: bash -c 'FILE="docs/cross-platform-research/maui-research.md" && test -f "$FILE" && grep -c "^## " "$FILE" | grep -q "^[8-9]\|[1-9][0-9]$" && ! grep -q "TBD\|TODO" "$FILE" && wc -w "$FILE" | awk "{if(\$1 >= 3000) exit 0; else exit 1}"'

## Files Likely Touched

- docs/cross-platform-research/avalonia-research.md
- docs/cross-platform-research/electron-net-research.md
- docs/cross-platform-research/blazor-hybrid-research.md
- docs/cross-platform-research/web-app-research.md
- docs/cross-platform-research/maui-research.md
