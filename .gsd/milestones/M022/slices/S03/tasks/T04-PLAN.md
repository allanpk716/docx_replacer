---
estimated_steps: 15
estimated_files: 2
skills_used: []
---

# T04: 撰写 .NET MAUI 跨平台方案文献调研文档

通过 Web 搜索和文档阅读，调研 .NET MAUI 的技术特性、跨平台支持（特别是 Linux/macOS 桌面支持）、与 DocuFiller 的适配性，并撰写完整调研文档。

调研内容必须覆盖：
1. 技术概述（MAUI 架构、Handlers 模式、单项目多目标、XAML 支持）
2. 与 DocuFiller 的适配性分析（XAML 迁移路径、MVVM 支持、服务层复用性、与 WPF 的 XAML 差异）
3. 跨平台支持状态（Windows 原生 WinUI 3、macOS 桌面支持、Linux 支持现状及社区方案）
4. NuGet 生态与依赖
5. .NET 8/9 兼容性
6. 打包与分发（MSIX/macOS app bundles、Velopack 集成可能性）
7. 社区活跃度与维护状态（Microsoft 官方投入、社区贡献）
8. 性能特征（启动时间、内存占用、WinUI 3 渲染）
9. 优缺点总结
10. 成熟度评估（1-5 分评分）
11. 调研日期与信息来源

关键调研方向：.NET MAUI 的核心限制是 Linux 桌面没有官方支持——这是 DocuFiller 跨平台迁移的关键阻碍。需重点调研：(a) 社区项目如 https://github.com/jsuarezruiz/maui-linux/ 的成熟度；(b) macOS 桌面支持的稳定程度；(c) MAUI 对 WinUI 3 的依赖意味着从 WPF 迁移仍然需要 XAML 重写。同时关注 .NET 9 中 MAUI 的改进。

参考已有调研文档格式：`docs/cross-platform-research/electron-net-research.md` 的章节结构和深度。

## Inputs

- `docs/cross-platform-research/electron-net-research.md`

## Expected Output

- `docs/cross-platform-research/maui-research.md`

## Verification

bash -c 'FILE="docs/cross-platform-research/maui-research.md" && test -f "$FILE" && grep -c "^## " "$FILE" | grep -q "^[8-9]\|[1-9][0-9]$" && ! grep -q "TBD\|TODO" "$FILE" && wc -w "$FILE" | awk "{if(\$1 >= 3000) exit 0; else exit 1}"'
