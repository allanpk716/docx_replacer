---
estimated_steps: 12
estimated_files: 1
skills_used: []
---

# T01: 调研 Velopack 跨平台能力并撰写 velopack-cross-platform.md

**Slice:** S04 — 通用课题调研（Velopack/核心库/平台差异/打包分发）
**Milestone:** M022

## Description

调研 Velopack 在 Windows、macOS、Linux 三平台上的自动更新能力。DocuFiller 当前使用 Velopack 0.0.1298（仅 Windows），需要了解跨平台迁移后 Velopack 能否继续作为统一更新框架。

调研内容必须覆盖：
1. 技术概述（Velopack 定位、与 Squirrel/Clowd.Squirrel 的关系、核心功能）
2. Windows 支持现状（当前 DocuFiller 的使用方式：UpdateManager、vpk pack、releases feed）
3. macOS 支持（.app bundle 打包、dmg 集成、Sparkle 协议兼容性、代码签名集成）
4. Linux 支持（AppImage 打包、deb/rpm 集成可行性、包管理器分发）
5. vpk CLI 跨平台能力（各平台打包命令差异、CI/CD 集成）
6. 跨平台 releases feed 格式（releases.win.json vs releases.mac.json vs releases.linux.json、通道机制）
7. 增量更新（Delta 更新在各平台的可用性、二进制 diff 算法差异）
8. 局限性与已知问题（各平台的成熟度差异、社区反馈）
9. 与替代方案对比（Sparkle for macOS、AppImageUpdate for Linux）
10. 对 DocuFiller 的建议（是否能统一三平台更新、需要哪些额外工具）
11. 优缺点总结
12. 调研日期与信息来源

关键调研方向：Velopack 官方声称支持 Windows/macOS/Linux，但 macOS 和 Linux 支持的实际成熟度需要深入验证。关注 vpk 在非 Windows 平台的打包能力、Sparkle 协议兼容性（macOS 更新的行业标准）、以及增量更新在非 Windows 平台的可靠性。

参考已有调研文档格式：`docs/cross-platform-research/avalonia-research.md` 的章节结构和深度（≥8 个编号章节、目录、表格对比、信息来源列表）。

注意：DocuFiller 当前使用 Velopack 的关键模式（来自项目记忆）：
- VelopackApp.Build().Run() 必须是 Program.Main() 的第一行
- 每个 API 方法创建独立的 UpdateManager 实例
- UpdateManager.ApplyUpdatesAndRestart() 需要传入 VelopackAsset 参数
- 不设置 ExplicitChannel 和 AllowVersionDowngrade（让 Velopack 使用 OS 默认通道）
- 使用 SimpleWebSource（内网）和 GithubSource（外网）双源
- 自定义 WPF 弹窗替代 Velopack 内置对话框

## Steps

1. 使用 web 搜索调研 Velopack 官方文档中关于 macOS 和 Linux 支持的内容
2. 查看 Velopack GitHub 仓库（velopack/velopack）的 README、issues、releases，确认跨平台支持状态
3. 调研 vpk CLI 在 macOS/Linux 上的打包命令和产物格式
4. 调研 Velopack 与 macOS Sparkle 更新协议的兼容性
5. 调研 Velopack 在 Linux 上与 AppImage 的集成方式
6. 调研 releases feed 格式在不同平台的差异
7. 分析 DocuFiller 当前 Velopack 使用方式在跨平台后的变化
8. 整理优缺点和成熟度评估
9. 撰写完整调研文档

## Must-Haves

- [ ] 文档覆盖 Velopack 在 Windows/macOS/Linux 三平台的更新能力
- [ ] 包含 vpk CLI 各平台打包命令差异
- [ ] 包含与 Sparkle/AppImageUpdate 的对比
- [ ] 包含对 DocuFiller 的具体建议
- [ ] 无 TBD/TODO 占位符

## Verification

- bash -c 'FILE="docs/cross-platform-research/velopack-cross-platform.md" && test -f "$FILE" && grep -c "^## " "$FILE" | grep -q "^[8-9]\|[1-9][0-9]$" && ! grep -q "TBD\|TODO" "$FILE" && wc -w "$FILE" | awk "{if(\$1 >= 3000) exit 0; else exit 1}"'

## Inputs

- `DocuFiller.csproj` — Velopack 0.0.1298 版本信息

## Expected Output

- `docs/cross-platform-research/velopack-cross-platform.md` — Velopack 跨平台能力调研文档
