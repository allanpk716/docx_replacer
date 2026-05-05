# Project

## What This Is

DocuFiller（原名 docx_replacer）是一个 WPF/C# 桌面应用，用于批量填充 Word 文档模板（通过 Excel 数据源）。支持 ContentControl 和文本替换、批注处理、页眉页脚，以及通过 Velopack 实现的自动更新（内网 Go 服务器 + GitHub Releases 双通道）。

## Core Value

让用户用 Excel 数据批量生成 Word 文档，保持原始模板的格式、样式和复杂排版不丢失。

## Project Shape

- **Complexity:** complex
- **Why:** WPF 桌面应用 + Go 更新服务器 + Velopack 打包/更新 + CLI 模式 + CI/CD 发布流水线

## Current State

- 核心功能完整：Excel 数据解析（两列/三列自动检测）、ContentControl 处理、文本替换、批注功能、清理功能、页眉页脚
- Velopack 自动更新完整：内网 Go 服务器 + GitHub Releases 双通道，安装版和便携版都支持
- CLI 模式：fill、inspect、update、cleanup 四个子命令，JSONL 输出
- CI/CD：GitHub Actions 自动构建发布到 GitHub Release，build-internal.bat 支持内网上传
- 跨平台研究已完成（Avalonia/MAUI/Electron 等），暂未实施
- 更新服务器（Go）当前嵌入在本项目的 update-server/ 子目录中，正在计划外置为独立项目（update-hub）

## Architecture / Key Patterns

- **技术栈：** C# / .NET 8 / WPF / EPPlus / DocumentFormat.OpenXml / Velopack
- **更新服务器：** Go 1.22，纯文件系统存储，NSSM Windows 服务
- **CLI：** 手写参数解析，AttachConsole(-1) P/Invoke，JSONL 输出
- **DI：** 手写服务注册（无 IoC 容器）
- **测试：** xUnit，单元测试 + 集成测试 + E2E 回归测试
- **打包：** Velopack（Setup.exe + Portable.zip + .nupkg 增量更新）
- **命名约定：** snake_case 文件名，PascalCase C# 代码

## Capability Contract

See `.gsd/REQUIREMENTS.md` for the explicit capability contract, requirement status, and coverage mapping.

## Milestone Sequence

- [x] M001-M022: 核心功能、CLI、更新系统、清理功能等已完成
- [ ] M023: update-hub 独立项目（Go 服务器 + Web UI + 数据迁移 + 部署）
- [ ] M024: DocuFiller 侧清理重构 + 多语言客户端示例 + 文档
