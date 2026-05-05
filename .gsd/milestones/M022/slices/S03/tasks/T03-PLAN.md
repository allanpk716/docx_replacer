---
estimated_steps: 15
estimated_files: 2
skills_used: []
---

# T03: 撰写纯 Web 应用方案文献调研文档

通过 Web 搜索和文档阅读，调研纯 Web 应用方案（ASP.NET Core 后端 + SPA/PWA 前端）作为 DocuFiller 跨平台方案的技术特性、可行性，并撰写完整调研文档。

调研内容必须覆盖：
1. 技术概述（ASP.NET Core 后端 + 前端框架选型：React/Vue/Blazor WebAssembly、PWA 模式、Electron Tauri 包装）
2. 与 DocuFiller 的适配性分析（后端服务层 100% 复用、UI 完全重写、文件系统访问限制、拖放支持）
3. 跨平台支持状态（浏览器即跨平台、PWA 离线能力、桌面集成程度）
4. 前端生态与依赖
5. .NET 8 兼容性
6. 部署模式（自托管 vs 云部署、安装 vs 在线访问、本地服务模式）
7. 社区活跃度与成熟度
8. 性能特征（网络开销、大文件处理、WebAssembly 性能瓶颈）
9. 优缺点总结
10. 成熟度评估（1-5 分评分）
11. 调研日期与信息来源

关键调研方向：纯 Web 方案的核心挑战是 DocuFiller 重度依赖本地文件系统（Word/Excel 文件读写、拖放文件），浏览器沙箱限制严重。需重点评估：本地文件访问的 workaround（File System Access API、本地后端服务、浏览器扩展）、大文件处理的性能表现、PWA 离线能力的局限。

参考已有调研文档格式：`docs/cross-platform-research/electron-net-research.md` 的章节结构和深度。

## Inputs

- `docs/cross-platform-research/electron-net-research.md`

## Expected Output

- `docs/cross-platform-research/web-app-research.md`

## Verification

bash -c 'FILE="docs/cross-platform-research/web-app-research.md" && test -f "$FILE" && grep -c "^## " "$FILE" | grep -q "^[8-9]\|[1-9][0-9]$" && ! grep -q "TBD\|TODO" "$FILE" && wc -w "$FILE" | awk "{if(\$1 >= 3000) exit 0; else exit 1}"'
