---
estimated_steps: 27
estimated_files: 1
skills_used: []
---

# T03: Write Tauri + .NET sidecar comprehensive research document

Write the complete Tauri + .NET sidecar research document at docs/cross-platform-research/tauri-dotnet-research.md. This is the primary deliverable for S05's cross-scheme comparison.

The document must cover (in Chinese, matching the Electron.NET research doc structure):

1. 技术概述: Tauri 是什么，架构（Rust 后端 + WebView 前端），与 Electron 的本质区别
2. 与 DocuFiller 的适配性分析: UI 层、后端层、CLI 层、文件对话框、进度汇报的迁移可行性
3. IPC 通信机制: Tauri commands vs sidecar HTTP vs stdin/stdout 三种 .NET 集成模式的对比
4. .NET Sidecar 模式: .NET 进程作为独立 sidecar 运行的架构分析，优缺点
5. NuGet 生态与依赖: DocumentFormat.OpenXml、EPPlus 等核心库在 .NET sidecar 中的使用
6. Tauri 生态与插件: tauri-plugin-dialog、tauri-plugin-shell 等，与 Velopack 的兼容性
7. 跨平台支持: Windows (WebView2)、macOS (WebKit)、Linux (WebKitGTK) 三平台情况
8. 打包与分发: Tauri bundler 能力（MSI/dmg/deb/AppImage），与 Velopack 的协作可能性
9. 社区活跃度与维护状态: GitHub stars、commit 频率、Tauri v2 稳定性
10. 性能特征: 内存占用（对比 Electron）、启动速度、安装包体积
11. 优缺点总结: SWOT 分析作为 DocuFiller 跨平台方案
12. 成熟度评估: TRL 判断
13. PoC 发现总结: 基于 T01/T02 的实际开发体验
14. 调研日期与信息来源

Constraints:
- 所有技术论断必须有据可查，标注来源
- PoC 相关结论基于实际代码验证
- 文档长度 3000-5000 字（中文）
- 格式和深度与 electron-net-research.md 保持一致

Must-Haves:
- [ ] 12+ 章节覆盖所有要求的主题
- [ ] 包含 PoC 实际开发发现（不是猜测）
- [ ] 包含 Tauri v2 vs Electron 的对比分析
- [ ] 包含 sidecar IPC 三种模式的对比
- [ ] 信息来源标注完整

## Inputs

- `poc/tauri-docufiller/src-tauri/Cargo.toml — PoC Rust dependencies for analysis`
- `poc/tauri-docufiller/src-tauri/tauri.conf.json — PoC Tauri config for architecture reference`
- `poc/tauri-docufiller/src-tauri/src/lib.rs — Tauri command patterns from T02`
- `poc/tauri-docufiller/sidecar-dotnet/Program.cs — Sidecar HTTP API patterns from T02`
- `poc/tauri-docufiller/sidecar-dotnet/sidecar-dotnet.csproj — .NET sidecar project for dependency analysis`
- `DocuFiller.csproj — Original project for dependency comparison`
- `docs/cross-platform-research/electron-net-research.md — Reference for document structure consistency`

## Expected Output

- `docs/cross-platform-research/tauri-dotnet-research.md — Complete Tauri + .NET sidecar research document with 12+ sections`

## Verification

test -f docs/cross-platform-research/tauri-dotnet-research.md && wc -l docs/cross-platform-research/tauri-dotnet-research.md
