# DocuFiller

## What This Is

DocuFiller 是一款基于 .NET 8 + WPF 的 Windows 桌面应用，通过 Excel 数据文件批量填充 Word 文档模板中的内容控件。支持富文本格式保留（上标、下标）、页眉页脚替换、批注追踪、审核清理（去除控件和批注）。提供 CLI 接口（JSONL 输出）供 LLM Agent 集成调用。具备 Velopack 驱动的内网自动更新能力（stable/beta 双通道）。

## Core Value

用户选择模板 + Excel 数据源 → 一键批量生成格式正确的 Word 文档。内容控件精确定位替换，不破坏文档结构。CLI 模式支持第三方 LLM agent 无需 GUI 直接调用核心功能。

## Current State

已完成八个里程碑：
- **M001-a60bo7**: Excel 三列格式支持（ID|关键词|值），自动格式检测，ID 唯一性校验
- **M002-ahlnua**: 代码质量清理（ILogger 注入、OpenFolderDialog 替换、Console.WriteLine 清除）
- **M003-g1w88x**: 文档全面更新（7 份文档对齐代码库、.trae/documents/ 迁移清理）
- **M004-l08k3s**: 功能瘦身 — 移除在线更新（19文件）、JSON编辑器遗留（9文件）、JSON数据源（IDataParser）、转换器窗口（5文件）、KeywordEditorUrl、Tools目录（10项目）、Newtonsoft.Json依赖，Excel成为唯一数据源，文档全部同步
- **M005-3te7t8**: CLI 接口 — 新增命令行接口（CliRunner + 3 子命令），JSONL 格式输出，37 个 CLI 单元测试，文档更新
- **M006-rj9bue**: 真实数据端到端回归测试 — 用真实业务数据创建独立 E2E 测试项目
- **M007-wpaxa3**: Velopack 自动更新 — 集成 Velopack 框架，Program.cs 初始化，更新服务（UpdateService），主窗口状态栏版本号+检查更新，build-internal.bat 发布脚本产出 Setup.exe + Portable.zip，E2E 测试脚本和测试指南
- **M008-4uyz6m**: 双通道更新系统 — Go 轻量更新服务器（上传/promote/列表/清理 API），客户端通道选择（appsettings.json Channel 字段），发布脚本一条命令构建+发布，端到端双通道验证通过

## Architecture / Key Patterns

- **框架**: .NET 8 + WPF，MVVM 模式
- **文档处理**: DocumentFormat.OpenXml（SafeTextReplacer 三种替换策略处理表格控件）
- **Excel 处理**: EPPlus 7.5.2（ExcelDataParserService 自动检测两列/三列格式）
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **CLI 接口**: CliRunner 参数解析 + fill/cleanup/inspect 子命令，JsonlOutput 统一 envelope 输出
- **自动更新**: Velopack（UpdateManager 检测/下载/安装/重启，vpk 打包发布）
- **更新通道**: stable/beta 双通道，通过 appsettings.json Channel 字段配置，Go 更新服务器托管（单二进制，文件系统存储，Bearer Token 认证，每通道保留 10 版本自动清理）
- **发布形态**: PublishSingleFile self-contained 单 EXE，Velopack 产出 Setup.exe + Portable.zip + 增量更新包
- **服务层**: 10+ 个服务接口覆盖文档处理、Excel 数据解析、格式化替换、文档清理、自动更新等核心功能
- **主界面**: 两个 Tab（关键词替换、审核清理）+ 底部状态栏（版本号 + 检查更新）
- **数据源**: 仅 Excel（.xlsx）
- **双模式启动**: 无参数→WPF GUI，有参数→CLI（JSONL 输出）

## Capability Contract

See `.gsd/REQUIREMENTS.md` for the explicit capability contract, requirement status, and coverage mapping.

## Milestone Sequence

- [x] M001-a60bo7: Excel 行 ID 列支持 — 三列 Excel 格式自动检测与 ID 唯一性校验
- [x] M002-ahlnua: 代码质量清理 — ILogger 注入、调试日志清除、OpenFolderDialog 替换
- [x] M003-g1w88x: 文档全面更新 — 7 份文档对齐代码库当前状态
- [x] M004-l08k3s: 功能瘦身 — 移除在线更新、JSON 相关、转换器、Tools 等不活跃模块
- [x] M005-3te7t8: CLI 接口 — LLM Agent 集成（fill/cleanup/inspect 子命令，JSONL 输出）
- [x] M006-rj9bue: 真实数据端到端回归测试
- [x] M007-wpaxa3: Velopack 自动更新 — 单 EXE 发布 + 内网更新 + 安装版/便携版
- [x] M008-4uyz6m: 双通道更新系统 — Go 更新服务器 + stable/beta 通道 + 自动化发布
