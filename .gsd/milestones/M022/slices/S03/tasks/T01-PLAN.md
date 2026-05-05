---
estimated_steps: 15
estimated_files: 2
skills_used: []
---

# T01: 撰写 Avalonia 跨平台方案文献调研文档

通过 Web 搜索和文档阅读，调研 Avalonia UI 框架的技术特性、跨平台支持、与 DocuFiller 的适配性，并撰写完整调研文档。

调研内容必须覆盖：
1. 技术概述（架构、XAML 支持、渲染引擎、版本信息）
2. 与 DocuFiller 的适配性分析（WPF XAML 迁移难度、MVVM 支持、服务层复用性）
3. 跨平台支持状态（Windows/Linux/macOS 支持程度、原生控件映射）
4. NuGet 生态与依赖（核心包、第三方库生态）
5. .NET 8 兼容性
6. 打包与分发（各平台安装包格式、Velopack 集成可能性）
7. 社区活跃度与维护状态（GitHub stars/contributors、最近发布、issue 响应速度）
8. 性能特征（启动时间、内存占用、渲染性能）
9. 优缺点总结
10. 成熟度评估（1-5 分评分）
11. 调研日期与信息来源

关键调研方向：Avalonia 的 XAML 兼容性是所有方案中与 WPF 最接近的，需重点评估 XAML 迁移的工作量和技术风险。关注 Avalonia 11.x 的成熟度和 Linux/macOS 上的渲染差异。

参考已有调研文档格式：`docs/cross-platform-research/electron-net-research.md` 的章节结构和深度。

## Inputs

- `docs/cross-platform-research/electron-net-research.md`

## Expected Output

- `docs/cross-platform-research/avalonia-research.md`

## Verification

bash -c 'FILE="docs/cross-platform-research/avalonia-research.md" && test -f "$FILE" && grep -c "^## " "$FILE" | grep -q "^[8-9]\|[1-9][0-9]$" && ! grep -q "TBD\|TODO" "$FILE" && wc -w "$FILE" | awk "{if(\$1 >= 3000) exit 0; else exit 1}"'
