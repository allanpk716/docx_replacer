# DocuFiller

## What This Is

DocuFiller 是一个基于 .NET 8 + WPF 的桌面应用程序，支持 GUI 和 CLI 双模式。提供 Word 文档批量填充、富文本替换、批注追踪和审核清理 4 大功能模块。CLI 模式输出 JSONL 格式，适合 LLM agent 集成。

## Core Value

用 Excel 数据驱动 Word 模板批量生成文档，保持格式完整，追踪变更痕迹。

## Current State

- 10 个服务接口 + 2 个处理器的完整业务层
- GUI（WPF MVVM）+ CLI（JSONL 输出）双模式运行
- 支持 Excel 两列/三列格式自动检测、正文/页眉/页脚内容控件替换
- Velopack 更新框架已集成，内网 Go 更新服务器已部署
- E2E 回归测试覆盖主要业务路径
- 在线更新功能全套移除并重建为 Velopack 方案（M005-M008）
- GitHub CI/CD 发布流水线已建立（v* tag 触发自动发布到 GitHub Release）
- UpdateService 多源切换（HTTP URL 优先 + GithubSource 备选），便携版检测
- GUI 状态栏常驻更新提示（便携版/有新版本/检查失败三种状态）
- CLI update 子命令（版本检查 JSONL + --yes 下载重启 + post-command 更新提醒）

## Architecture / Key Patterns

- MVVM + 依赖注入 + 分层架构（Singleton 服务为主，Transient 用于有状态组件）
- 双模式入口：`Program.Main` 检查 args 长度分流 CLI/GUI
- CLI 使用 `AttachConsole(-1)` P/Invoke 解决 WinExe stdout 问题
- Velopack `VelopackApp.Build().Run()` 必须在 Main 最先调用
- 更新服务：`UpdateService` 封装 Velopack `UpdateManager`，多源切换（HTTP URL / GithubSource），配置驱动源选择
- 便携版检测：`UpdateManager.IsInstalled` 构造时缓存，区分安装版和便携版
- Go 更新服务器：文件系统存储，stable/beta 双通道，Bearer Token 认证
- GitHub CI/CD：v* tag 触发 Actions workflow，Velopack 打包，Release 自动创建
- CLI post-command hook：成功命令后条件性追加更新提醒 JSONL

## Capability Contract

See `.gsd/REQUIREMENTS.md` for the explicit capability contract, requirement status, and coverage mapping.

## Milestone Sequence

- [x] M001: 三列 Excel 格式支持 — 自动检测两列/三列格式
- [x] M002: 文档迁移与清理 — 产品文档迁移到 docs/，清理在线更新代码
- [x] M003: MainWindow 布局重构 — 功能面板整合，拖拽支持
- [x] M004: JSON 数据源清理 — 移除 DataParserService，Excel 为唯一数据源
- [x] M005: Velopack 更新框架集成 — 替换旧更新方案
- [x] M006: E2E 回归测试 — 端到端测试覆盖主要业务路径
- [x] M007: 更新服务器 — Go 语言内网更新服务器
- [x] M008: Velopack 更新 UI — 自定义 WPF 更新弹窗
- [x] M009: GitHub CI/CD 发布 + 多源更新提醒 — tag 驱动发布流水线，GUI/CLI 更新体验
