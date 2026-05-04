---
estimated_steps: 15
estimated_files: 2
skills_used: []
---

# T02: 撰写 Blazor Hybrid 跨平台方案文献调研文档

通过 Web 搜索和文档阅读，调研 Blazor Hybrid（Blazor WebView + MAUI/Electron/WPF 宿主）的技术特性、跨平台支持、与 DocuFiller 的适配性，并撰写完整调研文档。

调研内容必须覆盖：
1. 技术概述（Blazor 架构、WebView 渲染、宿主选项：MAUI Blazor/WPF Blazor/Electron Blazor）
2. 与 DocuFiller 的适配性分析（WPF 宿主保留可能性、Razor 组件替代 WPF 控件、服务层复用性）
3. 跨平台支持状态（通过 MAUI Blazor 实现 Linux/macOS 的可行性、WebView2 依赖问题）
4. NuGet 生态与依赖
5. .NET 8 兼容性
6. 打包与分发
7. 社区活跃度与维护状态
8. 性能特征（WebView 开销、Blazor SSR vs WebAssembly 模式差异）
9. 优缺点总结
10. 成熟度评估（1-5 分评分）
11. 调研日期与信息来源

关键调研方向：Blazor Hybrid 的独特价值在于可以渐进迁移——先用 WPF Blazor WebView 保留现有窗口框架，逐步将 UI 迁移到 Razor 组件。需重点评估这种渐进路径的可行性。关注 Linux 上 WebView2 的替代方案（WebKitGTK）。

参考已有调研文档格式：`docs/cross-platform-research/electron-net-research.md` 的章节结构和深度。

## Inputs

- `docs/cross-platform-research/electron-net-research.md`

## Expected Output

- `docs/cross-platform-research/blazor-hybrid-research.md`

## Verification

bash -c 'FILE="docs/cross-platform-research/blazor-hybrid-research.md" && test -f "$FILE" && grep -c "^## " "$FILE" | grep -q "^[8-9]\|[1-9][0-9]$" && ! grep -q "TBD\|TODO" "$FILE" && wc -w "$FILE" | awk "{if(\$1 >= 3000) exit 0; else exit 1}"'
